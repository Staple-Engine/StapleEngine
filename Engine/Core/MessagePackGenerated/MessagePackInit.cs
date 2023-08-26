using MessagePack;
using MessagePack.Resolvers;

namespace Staple
{
    internal class MessagePackInit
    {
        private static bool serializerRegistered = false;

        public static void Initialize()
        {
            if (!serializerRegistered)
            {
                StaticCompositeResolver.Instance.Register(
                     GeneratedResolver.Instance,
                     StandardResolver.Instance
                );

                var option = MessagePackSerializerOptions.Standard
                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(StaticCompositeResolver.Instance)
                    .WithSecurity(MessagePackSecurity.UntrustedData);

                MessagePackSerializer.DefaultOptions = option;

                serializerRegistered = true;
            }
        }
    }
}
