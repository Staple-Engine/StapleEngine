#if IOS
using Foundation;
using CoreAnimation;
using ObjCRuntime;
using UIKit;

namespace Staple.Internal;

[Register("MetalView")]
public class MetalView : UIView
{
    public MetalView(NativeHandle handle) : base(handle)
    {
    }

    [Export("layerClass")]
    public static Class LayerClass()
    {
        return new Class(typeof(CAMetalLayer));
    }
}
#endif
