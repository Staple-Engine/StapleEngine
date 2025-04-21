using System.Collections.Generic;
using System.Linq;

namespace Staple.Networking;

public class NetworkBuffer<T>(int bufferSize, int correctionTollerance)
{
    public int Size => elements.Count;

    private int counter;

    private readonly Queue<T> elements = [];
    private readonly int bufferSize = bufferSize;
    private readonly int correctionTollerance = correctionTollerance;

    public void Add(T element)
    {
        elements.Enqueue(element);
    }

    public T[] Get()
    {
        int size = elements.Count - 1;

        if (size == bufferSize)
        {
            counter = 0;
        }

        if (size > bufferSize)
        {
            if (counter < 0)
            {
                counter = 0;
            }

            counter++;

            if (counter > correctionTollerance)
            {
                int amount = elements.Count - bufferSize;

                var temp = new T[amount];

                for (int i = 0; i < amount; i++)
                {
                    temp[i] = elements.Dequeue();
                }

                return temp;
            }
        }

        if (size < bufferSize)
        {
            if (counter > 0)
            {
                counter = 0;
            }

            counter--;

            if (-counter > correctionTollerance)
            {
                return [];
            }
        }

        if (elements.Any())
        {
            return [elements.Dequeue()];
        }

        return [];
    }
}
