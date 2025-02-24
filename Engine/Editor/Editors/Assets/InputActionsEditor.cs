using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(InputActions))]
public class InputActionsEditor : StapleAssetEditor
{
    public override void OnInspectorGUI()
    {
        if(target is not InputActions actions)
        {
            return;
        }

        EditorGUI.Label("Actions");

        EditorGUI.SameLine();

        EditorGUI.Button("+", "InputActionsEditor.AddAction", () =>
        {
            actions.actions.Add(new());
        });

        var changed = false;

        for (var i = 0; i < actions.actions.Count; i++)
        {
            var action = actions.actions[i];

            EditorGUI.Label($"Action {i + 1}");

            EditorGUI.Indent(() =>
            {
                action.name = EditorGUI.TextField("Name", $"InputActionsEditor.Action{i}.Name", action.name);

                action.type = EditorGUI.EnumDropdown("Type", $"InputActionsEditor.Action{i}.Type", action.type);

                EditorGUI.Label("Devices");

                EditorGUI.SameLine();

                EditorGUI.Button("+", $"InputActionsEditor.Action{i}.AddDevice", () =>
                {
                    action.devices.Add(new());
                });

                for (var j = 0; j < action.devices.Count; j++)
                {
                    var device = action.devices[j];

                    EditorGUI.Indent(() =>
                    {
                        EditorGUI.Label($"Device {j + 1}");

                        EditorGUI.SameLine();

                        EditorGUI.Button("-", $"InputActionsEditor.Action{i}.Device{j}.RemoveDevice", () =>
                        {
                            action.devices.RemoveAt(j);

                            changed = true;
                        });

                        device.device = EditorGUI.EnumDropdown("Device", $"InputActionsEditor.Action{i}.Device{j}.Type", device.device);
                        device.deviceIndex = EditorGUI.IntField("Device Index", $"InputActionsEditor.Action{i}.Device{j}.Index", device.deviceIndex);

                        switch (device.device)
                        {
                            case InputDevice.Keyboard:

                                device.keys ??= new();
                                device.gamepad = null;
                                device.mouse = null;
                                device.touch = null;

                                break;

                            case InputDevice.Gamepad:

                                device.keys = null;
                                device.gamepad ??= new();
                                device.mouse = null;
                                device.touch = null;

                                break;

                            case InputDevice.Mouse:

                                device.keys = null;
                                device.gamepad = null;
                                device.mouse ??= new();
                                device.touch = null;

                                break;

                            case InputDevice.Touch:

                                device.keys = null;
                                device.gamepad = null;
                                device.mouse = null;
                                device.touch ??= new();

                                break;
                        }

                        switch (device.device)
                        {
                            case InputDevice.Keyboard:

                                switch(action.type)
                                {
                                    case InputActionType.Axis:

                                        device.keys.firstPositive = EditorGUI.EnumDropdown("Positive", $"InputActionsEditor.Action{i}.Device{j}.FirstPositive",
                                            device.keys.firstPositive);

                                        device.keys.firstNegative = EditorGUI.EnumDropdown("Negative", $"InputActionsEditor.Action{i}.Device{j}.FirstNegative",
                                            device.keys.firstNegative);

                                        break;

                                    case InputActionType.Press:
                                    case InputActionType.ContinousPress:

                                        device.keys.firstPositive = EditorGUI.EnumDropdown("Key", $"InputActionsEditor.Action{i}.Device{j}.FirstPositive",
                                            device.keys.firstPositive);

                                        break;

                                    case InputActionType.DualAxis:

                                        device.keys.firstPositive = EditorGUI.EnumDropdown("X Positive", $"InputActionsEditor.Action{i}.Device{j}.FirstPositive",
                                            device.keys.firstPositive);

                                        device.keys.firstNegative = EditorGUI.EnumDropdown("X Negative", $"InputActionsEditor.Action{i}.Device{j}.FirstNegative",
                                            device.keys.firstNegative);

                                        device.keys.secondPositive = EditorGUI.EnumDropdown("Y Positive", $"InputActionsEditor.Action{i}.Device{j}.SecondPositive",
                                            device.keys.secondPositive);

                                        device.keys.secondNegative = EditorGUI.EnumDropdown("Y Negative", $"InputActionsEditor.Action{i}.Device{j}.SecondNegative",
                                            device.keys.secondNegative);

                                        break;
                                }

                                break;

                            case InputDevice.Gamepad:

                                switch (action.type)
                                {
                                    case InputActionType.Axis:

                                        device.gamepad.firstAxis = EditorGUI.EnumDropdown("Axis", $"InputActionsEditor.Action{i}.Device{j}.Axis",
                                            device.gamepad.firstAxis);

                                        break;

                                    case InputActionType.Press:
                                    case InputActionType.ContinousPress:

                                        device.gamepad.button = EditorGUI.EnumDropdown("Button", $"InputActionsEditor.Action{i}.Device{j}.Button",
                                            device.gamepad.button);

                                        break;

                                    case InputActionType.DualAxis:

                                        device.gamepad.firstAxis = EditorGUI.EnumDropdown("X Axis", $"InputActionsEditor.Action{i}.Device{j}.XAxis",
                                            device.gamepad.firstAxis);

                                        device.gamepad.secondAxis = EditorGUI.EnumDropdown("Y Axis", $"InputActionsEditor.Action{i}.Device{j}.YAxis",
                                            device.gamepad.secondAxis);

                                        break;
                                }

                                break;

                            case InputDevice.Mouse:

                                switch (action.type)
                                {
                                    case InputActionType.Axis:
                                    case InputActionType.DualAxis:

                                        device.mouse.scroll = EditorGUI.Toggle("Horizontal", $"InputActionsEditor.Action{i}.Device{j}.Scroll",
                                            device.mouse.scroll);

                                        device.mouse.horizontal = EditorGUI.Toggle("Horizontal", $"InputActionsEditor.Action{i}.Device{j}.Horizontal",
                                            device.mouse.horizontal);

                                        device.mouse.vertical = EditorGUI.Toggle("Horizontal", $"InputActionsEditor.Action{i}.Device{j}.Vertical",
                                            device.mouse.vertical);

                                        break;

                                    case InputActionType.Press:
                                    case InputActionType.ContinousPress:

                                        device.mouse.button = EditorGUI.EnumDropdown("Button", $"InputActionsEditor.Action{i}.Device{j}.Button",
                                            device.mouse.button);

                                        break;
                                }

                                break;

                            case InputDevice.Touch:

                                switch (action.type)
                                {
                                    case InputActionType.Axis:
                                    case InputActionType.DualAxis:

                                        device.touch.horizontal = EditorGUI.Toggle("Horizontal", $"InputActionsEditor.Action{i}.Device{j}.Horizontal",
                                            device.touch.horizontal);

                                        device.touch.vertical = EditorGUI.Toggle("Horizontal", $"InputActionsEditor.Action{i}.Device{j}.Vertical",
                                            device.touch.vertical);

                                        var area = new Vector4(device.touch.affectedArea.left, device.touch.affectedArea.top,
                                            device.touch.affectedArea.right, device.touch.affectedArea.bottom);

                                        area = EditorGUI.Vector4Field("Affected Area % (L,T,R,B)", $"InputActionsEditor.Action{i}.Device{j}.Vertical",
                                            area);

                                        device.touch.affectedArea = new(area.X, area.Z, area.Y, area.W);

                                        break;

                                    case InputActionType.Press:
                                    case InputActionType.ContinousPress:

                                        EditorGUI.Label("Change Device Index for Touch ID");

                                        break;
                                }

                                break;
                        }
                    });

                    if (changed)
                    {
                        break;
                    }
                }
            });

            if (changed)
            {
                break;
            }
        }

        ShowAssetUI(null);
    }
}
