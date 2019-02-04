using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Maverick.Json.TestObjects
{
    [Serializable]
    [DebuggerDisplay( "Count = {Count}" )]
    public class Index<TKey, TValue> : IList<TValue>, IList, IReadOnlyList<TValue>
    {
        private sealed class ValueIndex
        {
            public ValueIndex( Int32 index )
            {
                Index = index;
            }


            public Int32 Index { get; set; }
        }


        public Index( Func<TValue, TKey> selector, IEqualityComparer<TKey> comparer = null )
        {
            m_keySelector = selector;
            m_keys = new Dictionary<TKey, ValueIndex>( comparer ?? EqualityComparer<TKey>.Default );
            m_values = new List<TValue>();
        }


        public Int32 Count => m_values.Count;


        Boolean ICollection<TValue>.IsReadOnly => false;


        Boolean IList.IsReadOnly => false;


        Boolean IList.IsFixedSize => false;


        Object ICollection.SyncRoot => null;


        Boolean ICollection.IsSynchronized => false;


        public void AddRange( IEnumerable<TValue> values )
        {
            foreach ( var value in values )
            {
                Add( value );
            }
        }


        public void Add( TValue value )
        {
            var key = m_keySelector( value );

            if ( m_keys.ContainsKey( key ) )
            {
                throw new InvalidOperationException( $"The index already has a value with key {key}." );
            }

            m_values.Add( value );
            m_keys.Add( key, new ValueIndex( m_values.Count - 1 ) );
        }


        public Boolean RemoveKey( TKey key )
        {
            ValueIndex removedIndex;

            if ( m_keys.TryGetValue( key, out removedIndex ) )
            {
                m_values.RemoveAt( removedIndex.Index );
                m_keys.Remove( key );

                // Foreach index greater than the current, reduce by one
                foreach ( var valueIndex in m_keys.Values )
                {
                    if ( valueIndex.Index > removedIndex.Index )
                    {
                        --valueIndex.Index;
                    }
                }

                return true;
            }

            return false;
        }


        public Boolean Remove( TValue value )
        {
            var key = m_keySelector( value );

            return RemoveKey( key );
        }


        public void Clear()
        {
            m_keys.Clear();
            m_values.Clear();
        }


        public Boolean Contains( TValue value )
        {
            var key = m_keySelector( value );

            return m_keys.ContainsKey( key );
        }


        public Boolean ContainsKey( TKey key ) => m_keys.ContainsKey( key );


        /// <summary>
        /// Returns the item that corresponds to the provided key or default value if not found.
        /// </summary>
        public TValue Find( TKey key )
        {
            ValueIndex index;

            if ( m_keys.TryGetValue( key, out index ) )
            {
                return m_values[ index.Index ];
            }

            return default( TValue );
        }


        /// <summary>
        /// Returns the item that corresponds to the provided key or throws <see cref="KeyNotFoundException" /> if not found.
        /// </summary>
        public TValue Get( TKey key )
        {
            ValueIndex index;

            if ( !m_keys.TryGetValue( key, out index ) )
            {
                throw new KeyNotFoundException( $"No item with key {key} is found." );
            }

            return m_values[ index.Index ];
        }


        public Int32 IndexOf( TValue item )
        {
            ValueIndex index;
            var key = m_keySelector( item );

            if ( m_keys.TryGetValue( key, out index ) )
            {
                return index.Index;
            }

            return -1;
        }


        public List<TValue>.Enumerator GetEnumerator() => m_values.GetEnumerator();


        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => ( (IEnumerable<TValue>)m_values ).GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => ( (IEnumerable)m_values ).GetEnumerator();


        void ICollection<TValue>.CopyTo( TValue[] array, Int32 arrayIndex ) => m_values.CopyTo( array, arrayIndex );


        Object IList.this[ Int32 index ]
        {
            get
            {
                return m_values[ index ];
            }
            set
            {
                m_values[ index ] = (TValue)value;
            }
        }


        TValue IList<TValue>.this[ Int32 index ]
        {
            get
            {
                return m_values[ index ];
            }
            set
            {
                m_values[ index ] = value;
            }
        }


        TValue IReadOnlyList<TValue>.this[ Int32 index ] => m_values[ index ];


        public void RemoveAt( Int32 index )
        {
            var value = m_values[ index ];
            var key = m_keySelector( value );

            RemoveKey( key );
        }


        void IList<TValue>.Insert( Int32 index, TValue item )
        {
            throw new NotSupportedException();
        }


        Int32 IList.Add( Object value )
        {
            Add( (TValue)value );

            return Count - 1;
        }


        Boolean IList.Contains( Object value )
        {
            if ( s_isReferenceType && value == null )
            {
                return false;
            }

            return Contains( (TValue)value );
        }


        Int32 IList.IndexOf( Object value )
        {
            if ( s_isReferenceType && value == null )
            {
                return -1;
            }

            return IndexOf( (TValue)value );
        }


        void IList.Insert( Int32 index, Object value ) => ( (IList<TValue>)this ).Insert( index, (TValue)value );


        void IList.Remove( Object value ) => Remove( (TValue)value );


        void IList.RemoveAt( Int32 index ) => RemoveAt( index );


        void ICollection.CopyTo( Array array, Int32 index ) => ( (ICollection)m_values ).CopyTo( array, index );


        private readonly Func<TValue, TKey> m_keySelector;
        private readonly Dictionary<TKey, ValueIndex> m_keys;
        private readonly List<TValue> m_values;

        private static readonly Boolean s_isReferenceType = !typeof( TValue ).IsValueType;
    }


    public class Index<TValue> : Index<Int32, TValue> where TValue : IKey<Int32>
    {
        public Index() : base( x => x.Id )
        {
        }
    }


    public interface IKey<TKey>
    {
        TKey Id { get; }
    }


    public static class IndexExtensions
    {
        public static Index<TKey, TValue> ToIndex<TKey, TValue>( this IEnumerable<TValue> collection, Func<TValue, TKey> selector )
        {
            var index = new Index<TKey, TValue>( selector );
            index.AddRange( collection );

            return index;
        }



        public static Index<TValue> ToIndex<TValue>( this IEnumerable<TValue> collection ) where TValue : IKey<Int32>
        {
            var index = new Index<TValue>();
            index.AddRange( collection );

            return index;
        }
    }
}
