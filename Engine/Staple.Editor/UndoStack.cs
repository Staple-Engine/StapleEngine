using System;
using System.Collections.Generic;

namespace Staple.Editor;

internal class UndoStack
{
    public class Item
    {
        public string name;
        public Action perform;
        public Action revert;
    }

    private readonly List<Item> items = [];

    private int currentIndex = -1;

    public void Clear()
    {
        items.Clear();

        currentIndex = -1;
    }

    public void AddItem(string name, Action perform, Action revert)
    {
        if(currentIndex >= 0)
        {
            for (var i = items.Count - 1; i >= currentIndex; i--)
            {
                items.RemoveAt(i);
            }
        }

        items.Add(new()
        {
            name = name,
            perform = perform,
            revert = revert
        });

        currentIndex = items.Count;
    }

    public void Redo()
    {
        if(currentIndex >= items.Count)
        {
            return;
        }

        var item = items[currentIndex];

        currentIndex++;

        try
        {
            item.perform();
        }
        catch(Exception e)
        {
            Log.Debug($"[Redo] {e}");
        }
    }

    public void Undo()
    {
        if (currentIndex <= 0)
        {
            return;
        }

        var item = items[currentIndex - 1];

        currentIndex--;

        try
        {
            item.revert();
        }
        catch (Exception e)
        {
            Log.Debug($"[Undo] {e}");
        }
    }
}
