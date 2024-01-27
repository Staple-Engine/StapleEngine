#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#import <QuartzCore/QuartzCore.h>

id MacWindow(id handle)
{
    NSWindow *window = (NSWindow *)handle;
    
    CAMetalLayer *layer = [CAMetalLayer new];
    
    [window.contentView setLayer: layer];
    
    return layer;
}
