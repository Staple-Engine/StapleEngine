using Staple.Internal;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Asset for setting up input actions for an input observer
/// </summary>
public sealed class InputActions : IStapleAsset, IGuidAsset
{
    /// <summary>
    /// List of all actions
    /// </summary>
    public List<InputAction> actions = [];

    public GuidHasher Guid { get; } = new();

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadAsset<InputActions>(guid);
    }

    /// <summary>
    /// Registers all actions in this asset for a specific observer
    /// </summary>
    /// <returns>A list of action IDs to unregister later, if needed</returns>
    internal int[] RegisterActions(IInputReceiver receiver)
    {
        if ((actions?.Count ?? 0) == 0)
        {
            return [];
        }

        var outValue = new int[actions.Count];

        for (var i = 0; i < actions.Count; i++)
        {
            var action = actions[i];

            if (actions == null)
            {
                outValue[i] = -1;

                continue;
            }

            outValue[i] = action.type switch
            {
                InputActionType.Press or InputActionType.ContinousPress => Input.AddPressedAction(action,
                    (context) =>
                    {
                        receiver.OnPressed(context);
                    }),
                InputActionType.Axis => Input.AddSingleAxisAction(action,
                    (context, value) =>
                    {
                        receiver.OnAxis(context, value);
                    }),
                InputActionType.DualAxis => Input.AddDualAxisAction(action,
                    (context, value) =>
                    {
                        receiver.OnDualAxis(context, value);
                    }),
                _ => -1,
            };
        }

        return outValue;
    }
}
