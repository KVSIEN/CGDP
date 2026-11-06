using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // Everything the editor knows about node types: which can be created, where they sit
    // in the create menu, what they're called and how they're coloured. New node classes
    // appear automatically — tag them with [BtNodeInfo] for a menu path and description.
    internal static class BtNodeTypes
    {
        private static List<Type> _creatable;

        public static IReadOnlyList<Type> Creatable => _creatable ??= FindCreatable();

        public static string Path(Type type) =>
            Info(type)?.Path ?? $"Other/{ObjectNames.NicifyVariableName(type.Name)}";

        public static string DisplayName(Type type)
        {
            string path  = Path(type);
            int    slash = path.LastIndexOf('/');
            return slash >= 0 ? path[(slash + 1)..] : path;
        }

        public static string Description(Type type) => Info(type)?.Description ?? string.Empty;

        public static BtNodeCategory Category(BtNode node) => node switch
        {
            BtRoot      => BtNodeCategory.Root,
            BtComposite => BtNodeCategory.Composite,
            BtDecorator => BtNodeCategory.Decorator,
            BtCondition => BtNodeCategory.Condition,
            _           => BtNodeCategory.Action,
        };

        public static Color Color(BtNodeCategory category) => category switch
        {
            BtNodeCategory.Root      => new Color(0.30f, 0.30f, 0.32f),
            BtNodeCategory.Composite => new Color(0.20f, 0.38f, 0.62f),
            BtNodeCategory.Decorator => new Color(0.45f, 0.30f, 0.60f),
            BtNodeCategory.Condition => new Color(0.66f, 0.50f, 0.16f),
            _                        => new Color(0.22f, 0.50f, 0.30f),
        };

        public static Color StatusColor(BtStatus status) => status switch
        {
            BtStatus.Running => new Color(1.00f, 0.80f, 0.20f),
            BtStatus.Success => new Color(0.35f, 0.85f, 0.40f),
            _                => new Color(0.90f, 0.30f, 0.30f),
        };

        private static BtNodeInfoAttribute Info(Type type) =>
            (BtNodeInfoAttribute)Attribute.GetCustomAttribute(type, typeof(BtNodeInfoAttribute));

        // Public, concrete node classes other than the root (tests' private helpers stay out).
        private static List<Type> FindCreatable()
        {
            var types = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<BtNode>())
            {
                if (type.IsAbstract || type.IsGenericType || type == typeof(BtRoot)) continue;
                if (!type.IsPublic && !type.IsNestedPublic) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                types.Add(type);
            }
            types.Sort((a, b) => string.CompareOrdinal(Path(a), Path(b)));
            return types;
        }
    }
}
