using System;
using System.Reflection;
using UnityEngine;

namespace Needle.Engine.Core.References.MemberHandlers
{
    public class AudioSourceMembers : ITypeMemberHandler
    {
        public bool ShouldIgnore(Type currentType, MemberInfo member)
        {
            return false;
        }

        public bool ShouldRename(MemberInfo member, out string newName)
        {
            if (typeof(AudioSource).IsAssignableFrom(member.DeclaringType))
            {
                if (member.Name == "rolloffMode")
                {
                    newName = "rollOffMode";
                    return true;
                }
            }
            newName = null;
            return false;
        }
    }
}