﻿using Bgfx;
using System;
using System.Runtime.InteropServices;

namespace Staple;

internal class InstanceBuffer
{
    internal bgfx.InstanceDataBuffer buffer;
    internal readonly int stride;
    internal readonly int count;

    internal InstanceBuffer(bgfx.InstanceDataBuffer buffer, int stride, int count)
    {
        this.buffer = buffer;
        this.stride = stride;
        this.count = count;
    }

    public void Bind(int start, int count)
    {
        unsafe
        {
            fixed(bgfx.InstanceDataBuffer *p = &buffer)
            {
                bgfx.set_instance_data_buffer(p, (uint)start, (uint)count);
            }
        }
    }

    public void SetData(Span<byte> data)
    {
        if(data.Length != stride * count)
        {
            return;
        }

        unsafe
        {
            Span<byte> p = new Span<byte>(buffer.data, data.Length);

            data.CopyTo(p);
        }
    }

    public void SetData<T>(Span<T> data) where T: unmanaged
    {
        if (data.Length * Marshal.SizeOf<T>() != stride * count)
        {
            return;
        }

        unsafe
        {
            Span<T> p = new Span<T>(buffer.data, data.Length);

            data.CopyTo(p);
        }
    }

    public static int AvailableInstances(int requested, int stride)
    {
        return (int)bgfx.get_avail_instance_data_buffer((uint)requested, (ushort)stride);
    }

    public static InstanceBuffer Create(int requested, int stride)
    {
        requested = AvailableInstances(requested, stride);

        var buffer = new bgfx.InstanceDataBuffer();

        unsafe
        {
            bgfx.alloc_instance_data_buffer(&buffer, (uint)requested, (ushort)stride);
        }

        return new InstanceBuffer(buffer, stride, requested);
    }
}