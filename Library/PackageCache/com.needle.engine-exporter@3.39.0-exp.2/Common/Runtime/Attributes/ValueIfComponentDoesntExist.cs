using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Needle.Engine
{
    public class ValueIfComponentDoesntExist : PropertyAttribute
    {
        public Type type;
        [CanBeNull]
        public string propertyName;
        [CanBeNull]
        public string labelIfComponentExists;
        public MenuMode menuMode;

        public enum MenuMode
        {
            AddComponentButton,
            AddComponentMenu,
            None
        }
        
        public ValueIfComponentDoesntExist(Type type, string propertyName, string labelIfComponentExists = null, MenuMode menuMode = MenuMode.None)
        {
            this.type = type;
            this.propertyName = propertyName;
            this.labelIfComponentExists = labelIfComponentExists;
            this.menuMode = menuMode;
        }
    }
}
