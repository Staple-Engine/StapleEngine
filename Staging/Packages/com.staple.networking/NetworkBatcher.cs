using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Staple.Networking;

/// <summary>
/// Combines multiple network messages into a single one until a certain threshold passes and they are sent.
/// This is more efficient than sending multiple single messages due to overhead when doing so.
/// </summary>
public class NetworkBatcher
{
    /// <summary>
    /// Maximum byte size of the network batcher buffer
    /// </summary>
    public const int MaxSize = 1700;

    /// <summary>
    /// The status of decoding
    /// </summary>
    public enum DecodeStatus
    {
        /// <summary>
        /// Successfully decoded
        /// </summary>
        Success,

        /// <summary>
        /// There are no messages
        /// </summary>
        Empty,

        /// <summary>
        /// Data corrupted
        /// </summary>
        Corrupted
    }

    /// <summary>
    /// Represents a decoded message
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The message ID
        /// </summary>
        public ushort ID;

        /// <summary>
        /// The message data
        /// </summary>
        public byte[] data;
    }

    /// <summary>
    /// Walks through all the <see cref="Message"/> inside a byte array
    /// </summary>
    public class BatchWalker(byte[] buffer) : IEnumerable<Message>
    {
        private readonly byte[] buffer = buffer;
        private int position = 0;

        public IEnumerator<Message> GetEnumerator()
        {
            for (; ; )
            {
                if (Decode(buffer, ref position, out var message, out var data) == DecodeStatus.Success)
                {
                    yield return new Message()
                    {
                        ID = message,
                        data = data,
                    };
                }
                else
                {
                    yield break;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for(; ; )
            {
                if(Decode(buffer, ref position, out var message, out var data) == DecodeStatus.Success)
                {
                    yield return new Message()
                    {
                        ID = message,
                        data = data,
                    };
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    private readonly byte[] buffer = new byte[MaxSize];
    private int position = 0;

    public int Length
    {
        get
        {
            return position;
        }
    }

    public byte[] Buffer
    {
        get
        {
            return buffer;
        }
    }

    /// <summary>
    /// Encodes a message into a byte array
    /// </summary>
    /// <param name="message">The message type</param>
    /// <param name="data">The message data</param>
    /// <returns>The encoded data</returns>
    public static byte[] Encode(ushort message, byte[] data)
    {
        var sizeBytes = BitConverter.GetBytes(data.Length);

        int position = 0;
        byte[] buffer = new byte[data.Length + sizeof(ushort) + sizeof(byte) + sizeBytes.Length];

        buffer[position++] = (byte)message;
        buffer[position++] = (byte)(message >> 8);

        buffer[position++] = (byte)sizeBytes.Length;

        System.Buffer.BlockCopy(sizeBytes, 0, buffer, position, sizeBytes.Length);

        position += sizeBytes.Length;

        System.Buffer.BlockCopy(data, 0, buffer, position, data.Length);

        return buffer;
    }

    /// <summary>
    /// Decodes a message from a byte array
    /// </summary>
    /// <param name="inData">The data of the message</param>
    /// <param name="position">The position in the data</param>
    /// <param name="outMessage">The message ID we decode</param>
    /// <param name="outData">The message data we decode</param>
    /// <returns>The status of the decode</returns>
    public static DecodeStatus Decode(byte[] inData, ref int position, out ushort outMessage, out byte[] outData)
    {
        outMessage = 0;
        outData = [];

        if (position + sizeof(ushort) >= inData.Length)
        {
            return DecodeStatus.Empty;
        }

        outMessage = BitConverter.ToUInt16(inData, position);

        position += sizeof(ushort);

        if (position + sizeof(byte) >= inData.Length)
        {
            return DecodeStatus.Corrupted;
        }

        var dataSize = inData[position++];

        if (position + dataSize > inData.Length)
        {
            return DecodeStatus.Corrupted;
        }

        var dataSizeBytes = inData.Skip(position)
            .Take(dataSize)
            .ToArray();

        var dataLength = BitConverter.ToInt32(dataSizeBytes);

        position += dataSize;

        if (position + dataLength > inData.Length)
        {
            return DecodeStatus.Corrupted;
        }

        outData = inData.Skip(position)
            .Take(dataLength)
            .ToArray();

        position += dataLength;

        return DecodeStatus.Success;
    }

    /// <summary>
    /// Clears the message data
    /// </summary>
    public void Clear()
    {
        position = 0;
    }

    /// <summary>
    /// Attempts to batch a message into the buffer
    /// </summary>
    /// <param name="message">The message ID</param>
    /// <param name="data">The message data</param>
    /// <returns>Whether the message was batched, or false if it would become too large</returns>
    public bool Batch(ushort message, byte[] data)
    {
        var sizeVarint = BitConverter.GetBytes(data.Length);

        if(position + data.Length + sizeof(ushort) + sizeof(byte) + sizeVarint.Length > MaxSize)
        {
            return false;
        }

        var inData = Encode(message, data);

        Array.Copy(inData, 0, buffer, position, inData.Length);

        position += inData.Length;

        return true;
    }
}
