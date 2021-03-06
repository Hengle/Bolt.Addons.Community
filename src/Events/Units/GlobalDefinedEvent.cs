using Bolt.Addons.Community.DefinedEvents.Support;
using Bolt.Addons.Community.DefinedEvents.Support.Internal;
using Ludiq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace Bolt.Addons.Community.DefinedEvents.Units
{
    /// <summary>
    /// Listens for an event by type, rather than by name.  In other respects, it acts similar
    /// to the built-in Custom Unit.  This variation listens for events triggered by any
    /// Trigger Global Defined Event.
    /// </summary>
    [UnitCategory("Events")]
    [UnitTitle("Global Defined Event")]
    public class GlobalDefinedEvent : EventUnit<DefinedEventArgs>, IDefinedEventUnit
    {
        const string EventName = "OnGlobalDefinedEvent";

        #region Event Type Handling

        [SerializeAs(nameof(eventType))]
        private System.Type _eventType;


        /// <summary>
        /// The event type that will trigger this event.
        /// </summary>
        [DoNotSerialize]
        //[UnitHeaderInspectable("Event Type")]
        [InspectableIf(nameof(IsNotRestricted))]
        public System.Type eventType
        {
            get
            {
                return _eventType;
            }
            set
            {
                _eventType = value;
            }
        }

        /// <summary>
        /// The event type that will trigger this event.
        /// </summary>
        [DoNotSerialize]
        [UnitHeaderInspectable("Event Type")]
        [InspectableIf(nameof(IsRestricted))]
        [Ludiq.TypeFilter(TypesMatching.AssignableToAll, typeof(IDefinedEvent))]
        public System.Type restrictedEventType
        {
            get
            {
                return _eventType;
            }
            set
            {
                _eventType = value;
            }
        }

        public bool IsRestricted
        {
            get { return CommunityOptionFetcher.DefinedEvent_RestrictEventTypes; }
        }

        public bool IsNotRestricted
        {
            get { return !IsRestricted; }
        }
        #endregion


        [DoNotSerialize]
        public List<ValueOutput> outputPorts { get; } = new List<ValueOutput>();

        [DoNotSerialize]
        private ReflectedInfo Info;

        protected override bool register => true;

        protected override bool ShouldTrigger(Flow flow, DefinedEventArgs args)
        {
            return args.eventData.GetType() == eventType;
        }

        public override EventHook GetHook(GraphReference reference)
        {
            return ConstructHook(eventType);
        }

        protected override void Definition()
        {
            base.Definition();

            BuildFromInfo();
        }


        private void BuildFromInfo()
        {
            outputPorts.Clear();
            if (eventType == null)
                return;

            Info = ReflectedInfo.For(eventType);
            foreach (var field in Info.reflectedFields)
            {
                outputPorts.Add(ValueOutput(field.Value.FieldType, field.Value.Name));
            }

            foreach (var property in Info.reflectedProperties)
            {
                outputPorts.Add(ValueOutput(property.Value.PropertyType, property.Value.Name));
            }
        }

        protected override void AssignArguments(Flow flow, DefinedEventArgs args)
        {
            for (var i = 0; i < outputPorts.Count; i++)
            {
                var outputPort = outputPorts[i];
                var key = outputPort.key;
                if (Info.reflectedFields.ContainsKey(key))
                {
                    var reflectedField = Info.reflectedFields[key];
                    flow.SetValue(outputPort, reflectedField.GetValue(args.eventData));
                }
                else if (Info.reflectedProperties.ContainsKey(key))
                {
                    var reflectedProperty = Info.reflectedProperties[key];
                    flow.SetValue(outputPort, reflectedProperty.GetValue(args.eventData));
                }
            }
        }

        public static void Trigger(object eventData)
        {
            //var tag = eventData.GetType().GetTypeInfo().FullName;
            //var eventHook = new EventHook(EventName, null, tag);
            EventHook hook = ConstructHook(eventData.GetType());
            EventBus.Trigger(hook, new DefinedEventArgs(eventData));
        }

        private static EventHook ConstructHook(Type eventType)
        {
            EventHook hook;
            if (DefinedEventSupport.IsOptimized())
                hook = new EventHook(EventName, tag: eventType.GetTypeInfo().FullName);
            else
                hook = new EventHook(EventName);
            return hook;
        }
    }
}