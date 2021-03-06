﻿/*
 * The MIT License (MIT)
 * Copyright (c) StarX 2017
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CrazyStorm.Core
{
    public class EventManager
    {
        static List<EventExecutor> executorList;
        static Dictionary<string, Dictionary<string, TypeSet>> cache = new Dictionary<string, Dictionary<string, TypeSet>>();
        public static IList<ParticleType> DefaultTypes { get; set; }
        public static IList<ParticleType> CustomTypes { get; set; }
        public static void AddEvent(PropertyContainer propertyContainer, PropertyContainer bindingContainer, VMEventInfo eventInfo)
        {
            if (executorList == null)
                executorList = new List<EventExecutor>();

            var executor = new EventExecutor();
            executor.PropertyContainer = propertyContainer;
            executor.BindingContainer = bindingContainer;
            executor.PropertyName = eventInfo.resultProperty;
            executor.ChangeMode = eventInfo.changeMode;
            executor.ChangeTime = eventInfo.changeTime;
            propertyContainer.PushProperty(executor.PropertyName);
            var initialValue = new TypeSet();
            initialValue.type = eventInfo.resultType;
            var targetValue = eventInfo.resultValue;
            if (eventInfo.isExpressionResult)
            {
                VM.Execute(propertyContainer, eventInfo.resultExpression);
                switch (eventInfo.resultType)
                {
                    case PropertyType.Boolean:
                        targetValue.boolValue = VM.PopBool();
                        initialValue.boolValue = VM.PopBool();
                        break;
                    case PropertyType.Int32:
                        int resultInt = (int)VM.PopFloat();
                        initialValue.intValue = (int)VM.PopFloat();
                        if (eventInfo.changeType == EventKeyword.ChangeTo)
                            targetValue.intValue = resultInt;
                        else if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.intValue = initialValue.intValue + resultInt;
                        else
                            targetValue.intValue = initialValue.intValue - resultInt;

                        break;
                    case PropertyType.Single:
                        float resultFloat = VM.PopFloat();
                        initialValue.floatValue = VM.PopFloat();
                        if (eventInfo.changeType == EventKeyword.ChangeTo)
                            targetValue.floatValue = resultFloat;
                        else if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.floatValue = initialValue.floatValue + resultFloat;
                        else
                            targetValue.floatValue = initialValue.floatValue - resultFloat;

                        break;
                    case PropertyType.Enum:
                        targetValue.enumValue = VM.PopEnum();
                        initialValue.enumValue = VM.PopEnum();
                        break;
                    case PropertyType.Vector2:
                        Vector2 resultVector2 = VM.PopVector2();
                        initialValue.vector2Value = VM.PopVector2();
                        if (eventInfo.changeType == EventKeyword.ChangeTo)
                            targetValue.vector2Value = resultVector2;
                        else if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.vector2Value = initialValue.vector2Value + resultVector2;
                        else
                            targetValue.vector2Value = initialValue.vector2Value - resultVector2;

                        break;
                    case PropertyType.RGB:
                        RGB resultRGB = VM.PopRGB();
                        initialValue.rgbValue = VM.PopRGB();
                        if (eventInfo.changeType == EventKeyword.ChangeTo)
                            targetValue.rgbValue = resultRGB;
                        else if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.rgbValue = initialValue.rgbValue + resultRGB;
                        else
                            targetValue.rgbValue = initialValue.rgbValue - resultRGB;

                        break;
                    case PropertyType.String:
                        targetValue.stringValue = VM.PopString();
                        initialValue.stringValue = VM.PopString();
                        break;
                }
            }
            else
            {
                switch (eventInfo.resultType)
                {
                    case PropertyType.Boolean:
                        initialValue.boolValue = VM.PopBool();
                        break;
                    case PropertyType.Int32:
                        initialValue.intValue = VM.PopInt();
                        if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.intValue = initialValue.intValue + targetValue.intValue;
                        else if (eventInfo.changeType == EventKeyword.Decrease)
                            targetValue.intValue = initialValue.intValue - targetValue.intValue;

                        break;
                    case PropertyType.Single:
                        initialValue.floatValue = VM.PopFloat();
                        if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.floatValue = initialValue.floatValue + targetValue.floatValue;
                        else if (eventInfo.changeType == EventKeyword.Decrease)
                            targetValue.floatValue = initialValue.floatValue - targetValue.floatValue;

                        break;
                    case PropertyType.Enum:
                        initialValue.enumValue = VM.PopEnum();
                        break;
                    case PropertyType.Vector2:
                        initialValue.vector2Value = VM.PopVector2();
                        if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.vector2Value = initialValue.vector2Value + targetValue.vector2Value;
                        else if (eventInfo.changeType == EventKeyword.Decrease)
                            targetValue.vector2Value = initialValue.vector2Value - targetValue.vector2Value;

                        break;
                    case PropertyType.RGB:
                        initialValue.rgbValue = VM.PopRGB();
                        if (eventInfo.changeType == EventKeyword.Increase)
                            targetValue.rgbValue = initialValue.rgbValue + targetValue.rgbValue;
                        else if (eventInfo.changeType == EventKeyword.Decrease)
                            targetValue.rgbValue = initialValue.rgbValue - targetValue.rgbValue;

                        break;
                    case PropertyType.String:
                        initialValue.stringValue = VM.PopString();
                        break;
                }
            }
            executor.InitialValue = initialValue;
            executor.TargetValue = targetValue;
            if (executor.ChangeMode == EventKeyword.Instant)
            {
                executor.Update();
            }
            executorList.Add(executor);
        }
        public static bool ExecuteSpecialEvent(PropertyContainer propertyContainer, string eventName, string[] arguments,
            VMInstruction[] argumentExpression)
        {
            switch (eventName)
            {
                case "EmitParticle":
                    (propertyContainer as Emitter).EmitParticle();
                    break;
                case "PlaySound":
                    //TODO Sound
                    break;
                case "Loop":
                    VM.Execute(propertyContainer, argumentExpression);
                    if (!VM.PopBool())
                        return true;

                    break;
                case "ChangeType":
                    int typeId = int.Parse(arguments[0]) + int.Parse(arguments[1]);
                    if (typeId >= ParticleType.DefaultTypeIndex)
                    {
                        if (propertyContainer is Emitter)
                            (propertyContainer as Emitter).Template.Type = DefaultTypes[typeId - ParticleType.DefaultTypeIndex];
                        else if (propertyContainer is ParticleBase)
                            (propertyContainer as ParticleBase).Type = DefaultTypes[typeId - ParticleType.DefaultTypeIndex];
                    }
                    else
                    {
                        if (propertyContainer is Emitter)
                            (propertyContainer as Emitter).Template.Type = CustomTypes[typeId];
                        else if (propertyContainer is ParticleBase)
                            (propertyContainer as ParticleBase).Type = CustomTypes[typeId];
                    }
                    break;
            }
            return false;
        }
        public static void Update()
        {
            if (executorList == null)
                return;

            for (int i = 0; i < executorList.Count; ++i)
            {
                if (executorList[i].BindingContainer == null)
                {
                    if (executorList[i].Finished)
                    {
                        executorList.RemoveAt(i);
                        --i;
                    }
                    else
                        executorList[i].Update();
                }
            }
        }
        public static bool BindingUpdate(PropertyContainer propertyContainer, PropertyContainer bindingContainer)
        {
            if (executorList == null)
                return false;

            bool updated = false;
            string id = GetUniqueKey(propertyContainer, bindingContainer);
            for (int i = 0; i < executorList.Count; ++i)
            {
                if (executorList[i].PropertyContainer == propertyContainer && executorList[i].BindingContainer == bindingContainer)
                {
                    if (!cache.ContainsKey(id))
                    {
                        cache.Add(id, new Dictionary<string, TypeSet>());
                    }
                    if (!executorList[i].Finished)
                    {
                        executorList[i].Update();
                    }
                    cache[id][executorList[i].PropertyName] = executorList[i].CurrentValue;
                    if (executorList[i].Finished)
                    {
                        executorList.RemoveAt(i);
                        --i;
                    }
                    updated = true;
                }
            }
            return updated;
        }
        public static bool BindingRecover(PropertyContainer propertyContainer, PropertyContainer bindingContainer)
        {
            string id = GetUniqueKey(propertyContainer, bindingContainer);
            if (!cache.ContainsKey(id))
                return false;

            foreach (var item in cache[id])
            {
                switch (item.Value.type)
                {
                    case PropertyType.Boolean:
                        VM.PushBool(item.Value.boolValue);
                        break;
                    case PropertyType.Int32:
                        VM.PushFloat(item.Value.intValue);
                        break;
                    case PropertyType.Single:
                        VM.PushFloat(item.Value.floatValue);
                        break;
                    case PropertyType.Enum:
                        VM.PushEnum(item.Value.enumValue);
                        break;
                    case PropertyType.Vector2:
                        VM.PushVector2(item.Value.vector2Value);
                        break;
                    case PropertyType.RGB:
                        VM.PushRGB(item.Value.rgbValue);
                        break;
                    case PropertyType.String:
                        VM.PushString(item.Value.stringValue);
                        break;
                }
                propertyContainer.SetProperty(item.Key);
            }
            return true;
        }
        private static string GetUniqueKey(PropertyContainer propertyContainer, PropertyContainer bindingContainer)
        {
            return (propertyContainer as Component).ID + "_" + (bindingContainer as ParticleBase).ID;
        }
    }
}
