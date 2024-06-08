using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Needle.Engine.AdditionalData
{
    [HelpURL("https://rapier.rs/docs/user_guides/javascript/colliders#collision-groups-and-solver-groups")]
    public class ColliderData : AdditionalComponentData<Collider>
    {
        [Flags]
        public enum Group
        {
            Group_0 = 1 << 0,
            Group_1 = 1 << 1,
            Group_2 = 1 << 2,
            Group_3 = 1 << 3,
            Group_4 = 1 << 4,
            Group_5 = 1 << 5,
            Group_6 = 1 << 6,
            Group_7 = 1 << 7,
            Group_8 = 1 << 8,
            Group_9 = 1 << 9,
            Group_10 = 1 << 10,
            Group_11 = 1 << 11,
            Group_12 = 1 << 12,
            Group_13 = 1 << 13,
            Group_14 = 1 << 14,
            Group_15 = 1 << 15,
            Group_16 = 1 << 16,
        }

        [JsonIgnore, Info("The membership indicates what groups the collider is part of")] public Group _membership = (Group)Group.Group_0;
        [UsedImplicitly]
        public int[] membership {
            get
            {
                var result = new List<int>();
                for (var i = 0; i < 16; i++)
                    if ((_membership & (Group)(1 << i)) != 0)
                        result.Add(i);
                return result.ToArray();
            }
        }
        [JsonIgnore, Info("The filter indicates what groups the collider can interact with")] public Group _filter = (Group)~0;

        public int[] filter
        {
            get
            {
                if (_filter == (Group)~0) return null;
                var result = new List<int>();
                for (var i = 0; i < 16; i++)
                    if ((_filter & (Group)(1 << i)) != 0)
                        result.Add(i);
                return result.ToArray();
            }
        }
    }
}