using System;

namespace Staple.UI;

public class UIButton : UIInteractible
{
    public EntityCallback onClick = new();

    public override void Interact()
    {
        if(Clicked)
        {
            try
            {
                onClick.Invoke();
            }
            catch(Exception e)
            {
                Log.Error($"[Button] Click exception: {e}");
            }
        }
    }
}
