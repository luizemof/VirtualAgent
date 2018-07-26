using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualAgent.LUIS.ActionBinding.Attributes;
using VirtualAgent.LUIS.ActionBinding.Constants;

namespace VirtualAgent.LUIS.ActionBinding.Action
{
	[Serializable]
	internal class LuisActionResolver
	{
        private readonly IDictionary<string, Type> luisActions = new Dictionary<string, Type>();

        public LuisActionResolver(params Assembly[] lookupAssemblies)
        {
            lookupAssemblies.ThrowIfNull(nameof(lookupAssemblies));
            Initialize(lookupAssemblies);
        }

        private void Initialize(params Assembly[] lookupAssemblies)
        {
            foreach (var lookupAssembly in lookupAssemblies)
            {
                var attributes = lookupAssembly
                    .GetTypes()
                    .Select(t => new { Type = t, Attribs = t.GetCustomAttributes<LuisActionBindingAttribute>(true) })
                    .Where(o => o.Attribs.Any());

                foreach (var info in attributes)
                    foreach (var intentAttrib in info.Attribs)
                        this.luisActions.Add(intentAttrib.IntentName, info.Type);
            }
        }

        internal ILuisAction ResolveActionFromLuisIntent(LuisResult result, out string intentName)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            return this.ResolveActionFromLuisIntent(result, out intentName, out IList<EntityRecommendation> unassigned);
        }

        private ILuisAction ResolveActionFromLuisIntent(LuisResult result,
            out string intentName,
            out IList<EntityRecommendation> intentEntities,
            Func<PropertyInfo, IEnumerable<EntityRecommendation>, EntityRecommendation> entityExtractor = null)
        {
            intentEntities = default(IList<EntityRecommendation>);
            result.ThrowIfNull(nameof(result));

            // Has Intent?
            intentName = (result.TopScoringIntent ?? result.Intents?.MaxBy(i => i.Score ?? 0d)).Intent;
            if (string.IsNullOrWhiteSpace(intentName))
                return null;

            // Set out intent entities reference
            intentEntities = result.Entities;

            // Get Actions that map to this intent
            if (!this.luisActions.TryGetValue(intentName, out Type actionType))
                return null;

            // Get the action instance and check if it implements ILuisAction
            var luisAction = Activator.CreateInstance(actionType) as ILuisAction;
            if (luisAction == null)
                return null;

            // Try complete parameters from entities
            var properties = luisAction.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            AssignEntitiesToMembers(luisAction, properties, result.Entities, entityExtractor);

            return luisAction;
        }

        private static bool AssignEntitiesToMembers(
            ILuisAction action,
            IEnumerable<PropertyInfo> properties,
            IEnumerable<EntityRecommendation> entities,
            Func<PropertyInfo, IEnumerable<EntityRecommendation>, EntityRecommendation> entityExtractor = null)
        {
            var result = true;

            action.ThrowIfNull(nameof(action));
            properties.ThrowIfNull(nameof(properties));
            entities.ThrowIfNull(nameof(entities));

            if (!entities.Any())
                return false;

            CrossEntities(entities);
            result = FindEntity(action, properties, entities, entityExtractor);

            return result;
        }

        private static bool FindEntity(ILuisAction action, IEnumerable<PropertyInfo> properties, IEnumerable<EntityRecommendation> entities, Func<PropertyInfo, IEnumerable<EntityRecommendation>, EntityRecommendation> entityExtractor)
        {
            bool result = true;
            foreach (var property in properties)
            {
                var matchingEntity = default(EntityRecommendation);
                var matchingEntities = default(IEnumerable<EntityRecommendation>);
                var paramAttrib = property.GetCustomAttributes<LuisActionBindingParamAttribute>(true).FirstOrDefault();

                // Find using custom type from attrib if available
                if (paramAttrib != null)
                    matchingEntities = entities.Where(e => e.Type == paramAttrib.CustomType);

                // Find using property name
                if (matchingEntities != null || !matchingEntities.Any())
                    matchingEntities = entities.Where(e => e.Type == property.Name);

                // Find using builtin type from attrib if available
                if ((matchingEntities != null || !matchingEntities.Any()) && paramAttrib != null)
                    matchingEntities = entities.Where(e => e.Type == paramAttrib.BuiltinType);

                // If callback available then use it
                if (matchingEntities.Count() > 1)
                    matchingEntity = entityExtractor?.Invoke(property, matchingEntities);
                else
                    matchingEntity = matchingEntities.FirstOrDefault();

                // Prioritize resolution
                if (matchingEntity != null)
                {
                    var paramValue = matchingEntity.Resolution != null && matchingEntity.Resolution.Count > 0
                        ? matchingEntity.Resolution.First().Value
                        : matchingEntity.Entity;

                    result &= AssignValue(action, property, paramValue);
                }
                else if (matchingEntities.Count() > 0
                    &&
                    matchingEntities.Count(e => e.Resolution != null && e.Resolution.First().Value is JArray) == matchingEntities.Count())
                {
                    var paramValues = new JArray();
                    foreach (var currentMatchingEntity in matchingEntities)
                    {
                        var values = currentMatchingEntity.Resolution.First().Value as JArray;
                        foreach (var value in values)
                            paramValues.Add(value);
                    }

                    result &= AssignValue(action, property, paramValues);
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        private static bool AssignValue(ILuisAction action, PropertyInfo property, object paramValue)
        {
            action.ThrowIfNull(nameof(action));
            property.ThrowIfNull(nameof(property));
            paramValue.ThrowIfNull(nameof(paramValue));

            if (property != null && property.CanWrite)
            {
                // nullable support
                var type = property.PropertyType;
                type = Nullable.GetUnderlyingType(type) ?? type;

                try
                {
                    object value;
                    // handle LUIS JObjects
                    paramValue = SanitizeInputValue(type, paramValue);

                    if (type.IsArray)
                    {
                        value = BuildArrayOfValues(action, property, type.GetElementType(), paramValue);
                    }
                    else if (type.IsEnum)
                    {
                        value = Enum.Parse(type, paramValue.ToString());
                    }
                    else
                    {
                        value = Convert.ChangeType(paramValue, type);
                    }

                    property.SetValue(action, value);

                    return true;
                }
                catch (FormatException)
                {
                    // Handle invalid values (Eg. Try Parse '2017' as a Date will fail)
                }
            }

            return false;
        }

        private static object SanitizeInputValue(Type targetType, object value)
        {
            object result = value;
            // handle case where input is JArray returned from LUIS
            if (value is JArray arrayOfValues)
            {
                if (targetType.IsArray)
                    result = arrayOfValues.AsEnumerable<object>();
                else
                {
                    if (arrayOfValues.Count > 1)
                        throw new FormatException("Cannot assign multiple values to single field");

                    result = arrayOfValues[0];
                }
            }

            return result;
        }

        private static Array BuildArrayOfValues(ILuisAction action, PropertyInfo property, Type elementType, object paramValue)
        {
            var values = default(IEnumerable<object>);

            if (paramValue is IEnumerable<object>)
                values = paramValue as IEnumerable<object>;
            else
                values = paramValue.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim());

            if (values.Count() > 0)
            {
                var idx = 0;
                var result = Array.CreateInstance(elementType, values.Count());
                foreach (var value in values)
                    result.SetValue(elementType.IsEnum ? Enum.Parse(elementType, value.ToString()) : Convert.ChangeType(value, elementType), idx++);
                return result;
            }
            else
                return null;
        }

        private static void CrossEntities(IEnumerable<EntityRecommendation> entities)
        {
            // Cross match entities to copy resolution values for custom entities from pairs
            foreach (var group in entities.GroupBy(e => e.Entity))
            {
                if (group.Count() > 1)
                {
                    var entityToUpdate = group.FirstOrDefault(e => !BuiltInTypes.IsBuiltInType(e.Type));
                    var entityWithValue = group.FirstOrDefault(e => e.Resolution != null);
                    if (entityToUpdate != null && entityWithValue != null)
                        entityToUpdate.Resolution = entityWithValue.Resolution;
                }
            }
        }

        public static bool IsContextualAction(ILuisAction action)
        {
            action.ThrowIfNull(nameof(action));
            return GetActionInterface(action).Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILuisContextualAction<>));
        }

        public static bool CanStartWithNoContextAction(ILuisAction action, out LuisActionBindingAttribute actionDefinition)
        {
            action.ThrowIfNull(nameof(action));
            if (!IsContextualAction(action))
            {
                actionDefinition = null;
                return true;
            }

            actionDefinition = GetActionDefinition(action);

            return actionDefinition.CanExecuteWithNoContext;
        }

        public static LuisActionBindingAttribute GetActionDefinition(ILuisAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return action.GetType().GetCustomAttributes<LuisActionBindingAttribute>(true).FirstOrDefault();
        }

        public static ILuisAction BuildContextForContextualAction(ILuisAction action, out string intentName)
        {
            action.ThrowIfNull(nameof(action));

            if (!IsContextualAction(action))
            {
                intentName = null;
                return null;
            }

            var contextType = GetActionInterface(action)
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILuisContextualAction<>))
                .GetGenericArguments()[0];

            var result = Activator.CreateInstance(contextType) as ILuisAction;
            intentName = LuisActionResolver.GetActionDefinition(result).IntentName;

            var isContextual = false;
            LuisActionResolver.IsValidContextualAction(action, result, out isContextual);

            return result;
        }

        public static bool IsValidContextualAction(ILuisAction action, ILuisAction context, out bool isContextual)
        {
            action.ThrowIfNull(nameof(action));
            context.ThrowIfNull(nameof(context));

            isContextual = IsContextualAction(action);
            if (isContextual)
            {
                var validContextualType = typeof(ILuisContextualAction<>).MakeGenericType(context.GetType());
                if (validContextualType.IsAssignableFrom(action.GetType()))
                {
                    var prop = validContextualType.GetProperty("Context", BindingFlags.Public | BindingFlags.Instance);
                    prop.SetValue(action, context);

                    return true;
                }
            }

            return false;
        }

        private static Type[] GetActionInterface(ILuisAction action)
        {
            return action.GetType().GetInterfaces();
        }

        public static async Task<QueryValueResult> QueryValueFromLuisAsync(
           ILuisService service,
           ILuisAction action,
           string paramName,
           object paramValue,
           CancellationToken token,
           Func<PropertyInfo, IEnumerable<EntityRecommendation>, EntityRecommendation> entityExtractor = null)
        {
            var originalValue = paramValue;
            service.ThrowIfNull(nameof(service));
            action.ThrowIfNull(nameof(action));
            service.ThrowIfNull(nameof(service));
            paramValue.ThrowIfNull(nameof(paramValue));

            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentNullException(nameof(paramName));

            var result = await service.QueryAsync(paramValue.ToString(), token);
            var queryIntent = result.Intents.FirstOrDefault();
            if (!Intents.None.Equals(queryIntent.Intent, StringComparison.InvariantCultureIgnoreCase))
            {
                var newAction = new LuisActionResolver(action.GetType().Assembly).ResolveActionFromLuisIntent(result, out string newIntentName);
                if (newAction != null && !newAction.GetType().Equals(action.GetType()))
                {
                    return new QueryValueResult(false)
                    {
                        NewAction = newAction,
                        NewIntent = newIntentName
                    };
                }
            }

            var properties = new List<PropertyInfo>
            {
                action.GetType().GetProperty(paramName, BindingFlags.Public | BindingFlags.Instance)
            };

            if (!AssignEntitiesToMembers(action, properties, result.Entities, entityExtractor))
                return new QueryValueResult(AssignValue(action, properties.First(), originalValue));

            return new QueryValueResult(true);
        }

        public static bool UpdateIfValidContextualAction(ILuisAction action, ILuisAction context, out bool isContextual)
        {
            action.ThrowIfNull(nameof(action));
            context.ThrowIfNull(nameof(context));

            isContextual = false;
            if (!IsContextualAction(context))
                return true;

            var prop = context.GetType().GetProperty("Context", BindingFlags.Public | BindingFlags.Instance);
            var actionContext = prop.GetValue(context) as ILuisAction;

            return IsValidContextualAction(action, actionContext, out isContextual);
        }

        public static LuisActionBindingParamAttribute GetParameterDefinition(ILuisAction action, string paramName)
        {
            action.ThrowIfNull(nameof(action));

            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentNullException(nameof(paramName));

            var prop = action.GetType().GetProperty(paramName, BindingFlags.Public | BindingFlags.Instance);
            // search binding attrib
            return prop.GetCustomAttributes<LuisActionBindingParamAttribute>(true).FirstOrDefault();
        }
    }
}
