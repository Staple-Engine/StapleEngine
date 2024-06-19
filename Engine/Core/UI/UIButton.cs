using System;

namespace Staple.UI;

public class UIButton : UIInteractible
{
    public Action onClick;

    public override void Interact()
    {
        if(Clicked)
        {
            try
            {
                onClick?.Invoke();
            }
            catch(Exception e)
            {
                Log.Error($"[Button] Click exception: {e}");
            }
        }
    }
}
