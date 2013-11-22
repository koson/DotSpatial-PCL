/* Copyright � 2002-2004 by Aidant Systems, Inc., and by Jason Smith. */
using System;
using System.Collections.Generic;

namespace Iesi_NTS.Collections.Generic
{
	/// <summary>
	/// <p><c>DictionarySet</c> is an abstract class that supports the creation of new <c>Set</c>
	/// types where the underlying data store is an <c>IDictionary</c> instance.</p> 
	///  
	/// <p>You can use any object that implements the <c>IDictionary</c> interface to hold set data.
	/// You can define your own, or you can use one of the objects provided in the Framework.   
	/// The type of <c>IDictionary</c> you choose will affect both the performance and the behavior
	/// of the <c>Set</c> using it. </p>
	///
	/// <p>To make a <c>Set</c> typed based on your own <c>IDictionary</c>, simply derive a
	/// new class with a constructor that takes no parameters.  Some <c>Set</c> implmentations
	/// cannot be defined with a default constructor.  If this is the case for your class, 
	/// you will need to override <c>Clone()</c> as well.</p>
	///
	/// <p>It is also standard practice that at least one of your constructors takes an <c>ICollection</c> or 
	/// an <c>ISet</c> as an argument.</p>
	/// </summary>
	[Serializable]
    public abstract class DictionarySet<T> : Set<T>
	{
		/// <summary>
		/// Provides the storage for elements in the <c>Set</c>, stored as the key-set
		/// of the <c>IDictionary</c> object.  Set this object in the constructor
		/// if you create your own <c>Set</c> class.  
		/// </summary>
        protected IDictionary<T, object> InternalDictionary = null;
		private static readonly object PlaceholderObject = new object();

		/// <summary>
		/// The placeholder object used as the value for the <c>IDictionary</c> instance.
		/// </summary>
		/// <remarks>
		/// There is a single instance of this object globally, used for all <c>Sets</c>.
		/// </remarks>
		protected object Placeholder
		{
			get	{return PlaceholderObject;}
		}


		/// <summary>
		/// Adds the specified element to this set if it is not already present.
		/// </summary>
		/// <param name="o">The object to add to the set.</param>
		/// <returns><c>true</c> is the object was added, <c>false</c> if it was already present.</returns>
		public override bool Add(T o)
		{
			//The object we are adding is just a placeholder.  The thing we are
			//really concerned with is 'o', the key.
            if (!InternalDictionary.ContainsKey(o))
            {
                InternalDictionary.Add(o, PlaceholderObject);
                return true;
            }
            else return false;           
		}

		/// <summary>
		/// Adds all the elements in the specified collection to the set if they are not already present.
		/// </summary>
		/// <param name="c">A collection of objects to add to the set.</param>
		/// <returns><c>true</c> is the set changed as a result of this operation, <c>false</c> if not.</returns>
		public override bool AddAll(ICollection<T> c)
		{
			bool changed = false;
            foreach ( T o in c )
            {
                if ( ! this.Contains( o ) )
                {
                    this.Add( o );
                    changed = true;
                }
            }
			return changed;
		}

		/// <summary>
		/// Removes all objects from the set.
		/// </summary>
		public override void Clear()
		{
			InternalDictionary.Clear();
		}

		/// <summary>
		/// Returns <c>true</c> if this set contains the specified element.
		/// </summary>
		/// <param name="o">The element to look for.</param>
		/// <returns><c>true</c> if this set contains the specified element, <c>false</c> otherwise.</returns>
		public override bool Contains(T o)
		{            
			return InternalDictionary.ContainsKey( o );
		}

		/// <summary>
		/// Returns <c>true</c> if the set contains all the elements in the specified collection.
		/// </summary>
		/// <param name="c">A collection of objects.</param>
		/// <returns><c>true</c> if the set contains all the elements in the specified collection, <c>false</c> otherwise.</returns>
        public override bool ContainsAll( ICollection<T> c )
		{
			foreach(T o in c)
			{
				if( ! this.Contains(o) )
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns <c>true</c> if this set contains no elements.
		/// </summary>
		public override bool IsEmpty
		{
			get{return InternalDictionary.Count == 0;}
		}

		/// <summary>
		/// Removes the specified element from the set.
		/// </summary>
		/// <param name="o">The element to be removed.</param>
		/// <returns><c>true</c> if the set contained the specified element, <c>false</c> otherwise.</returns>
		public override bool Remove(T o)
		{
			bool contained = this.Contains(o);
			if(contained)
			{
				InternalDictionary.Remove(o);
			}
			return contained;
		}

		/// <summary>
		/// Remove all the specified elements from this set, if they exist in this set.
		/// </summary>
		/// <param name="c">A collection of elements to remove.</param>
		/// <returns><c>true</c> if the set was modified as a result of this operation.</returns>
        public override bool RemoveAll( ICollection<T> c )
		{
			bool changed = false;
			foreach(T o in c)
				changed |= this.Remove(o);
			return changed;
		}

		/// <summary>
		/// Retains only the elements in this set that are contained in the specified collection.
		/// </summary>
		/// <param name="c">Collection that defines the set of elements to be retained.</param>
		/// <returns><c>true</c> if this set changed as a result of this operation.</returns>
        public override bool RetainAll( ICollection<T> c )
		{
			//Put data from C into a set so we can use the Contains() method.
            Set<T> cSet = new HybridSet<T>( c );

			//We are going to build a set of elements to remove.
            Set<T> removeSet = new HybridSet<T>();
			
			foreach(T o in this)
			{
				//If C does not contain O, then we need to remove O from our
				//set.  We can't do this while iterating through our set, so
				//we put it into RemoveSet for later.
				if(!cSet.Contains(o))
					removeSet.Add(o);
			}

			return this.RemoveAll(removeSet);
		}


		/// <summary>
		/// Copies the elements in the <c>Set</c> to an array.  The type of array needs
		/// to be compatible with the objects in the <c>Set</c>, obviously.
		/// </summary>
		/// <param name="array">An array that will be the target of the copy operation.</param>
		/// <param name="index">The zero-based index where copying will start.</param>
        public override void CopyTo( T[] array, int index )
		{
			InternalDictionary.Keys.CopyTo(array, index);
		}

		/// <summary>
		/// The number of elements contained in this collection.
		/// </summary>
		public override int Count
		{
			get{return InternalDictionary.Count;}		
		}

		
		/// <summary>
		/// Gets an enumerator for the elements in the <c>Set</c>.
		/// </summary>
		/// <returns>An <c>IEnumerator</c> over the elements in the <c>Set</c>.</returns>
        public override IEnumerator<T> GetEnumerator()
		{
			return InternalDictionary.Keys.GetEnumerator();
		}

        public override bool IsSynchronized
        {
            get { return false; }
        }

        public override object SyncRoot
        {
            get { throw new NotImplementedException( "Implement me!" ); }
        }

      
	}
}
