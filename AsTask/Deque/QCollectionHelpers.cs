using System;
using System.Collections;
using System.Collections.Generic;

namespace HardDev.AsTask.Deque
{
    internal static class QCollectionHelpers
    {
        public static IReadOnlyCollection<T> ReifyCollection<T>(IEnumerable<T> source)
        {
            switch (source)
            {
                case null:
                    throw new ArgumentNullException(nameof(source));
                case IReadOnlyCollection<T> result:
                    return result;
                case ICollection<T> collection:
                    return new CollectionWrapper<T>(collection);
                case ICollection nonGenericCollection:
                    return new NonGenericCollectionWrapper<T>(nonGenericCollection);
                default:
                    return new List<T>(source);
            }
        }

        private sealed class NonGenericCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection _collection;

            public NonGenericCollectionWrapper(ICollection collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (T item in _collection)
                    yield return item;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }

        private sealed class CollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection<T> _collection;

            public CollectionWrapper(ICollection<T> collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }
    }
}