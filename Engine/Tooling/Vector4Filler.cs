using Staple.Internal;

namespace Staple.Tooling;

public class Vector4Filler
{
    public float x;
    public float y;
    public float z;
    public float w;

    public int index = 0;

    public float this[int index]
    {
        get
        {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                3 => w,
                _ => 0,
            };
        }

        set
        {
            switch (index)
            {
                case 0:

                    x = value;

                    break;

                case 1:

                    y = value;

                    break;

                case 2:

                    z = value;

                    break;

                case 3:

                    w = value;

                    break;
            }
        }
    }

    public void Add(float value)
    {
        switch (index++)
        {
            case 0:

                x = value;

                break;

            case 1:

                y = value;

                break;

            case 2:

                z = value;

                break;

            case 3:

                w = value;

                break;

            default:

                break;
        }
    }

    public Vector4Holder ToHolderNormalized()
    {
        var total = x + y + z + w;

        return new()
        {
            x = x / total,
            y = y / total,
            z = z / total,
            w = w / total,
        };
    }

    public Vector4Holder ToHolder()
    {
        return new()
        {
            x = x,
            y = y,
            z = z,
            w = w,
        };
    }
}
