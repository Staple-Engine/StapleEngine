namespace Staple
{
    public struct Entity
    {
        public int ID;
        public int generation;

        public readonly static Entity Empty = new Entity()
        {
            ID = -1,
            generation = 0,
        };

        public static bool operator==(Entity a, Entity b)
        {
            return a.ID == b.ID && a.generation == b.generation;
        }

        public static bool operator!=(Entity a, Entity b)
        {
            return (a == b) == false;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(obj, null))
            {
                return false;
            }

            if(obj is Entity entity)
            {
                return this == entity;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode() * 17 + generation.GetHashCode();
        }

        public override string ToString()
        {
            return $"Entity ({ID} {generation})";
        }
    }
}
