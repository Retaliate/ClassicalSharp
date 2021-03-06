﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using ClassicalSharp.Model;
using OpenTK;

namespace ClassicalSharp.Entities {
	
	/// <summary> Contains a model, along with position, velocity, and rotation.
	/// May also contain other fields and properties. </summary>
	public abstract partial class Entity {
		
		/// <summary> Returns the bounding box that contains the model, assuming it is not rotated. </summary>
		public AABB PickingBounds {
			get { UpdateModel(); AABB bb = Model.PickingBounds;
				return bb.Scale( ModelScale ).Offset( Position );
			}
		}
		
		/// <summary> Bounding box of the model that collision detection
		/// is performed with, in world coordinates.  </summary>
		public virtual AABB CollisionBounds {
			get { return AABB.Make( Position, CollisionSize ); }
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity satisfy the given condition. </summary>
		public bool TouchesAny( Predicate<byte> condition ) {
			return TouchesAny( CollisionBounds, condition );
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// given bounding box satisfy the given condition. </summary>
		public bool TouchesAny( AABB bounds, Predicate<byte> condition ) {
			Vector3I bbMin = Vector3I.Floor( bounds.Min );
			Vector3I bbMax = Vector3I.Floor( bounds.Max );
			
			// Order loops so that we minimise cache misses
			for( int y = bbMin.Y; y <= bbMax.Y; y++ )
				for( int z = bbMin.Z; z <= bbMax.Z; z++ )
					for( int x = bbMin.X; x <= bbMax.X; x++ )
			{
				if( !game.World.IsValidPos( x, y, z ) ) continue;
				byte block = game.World.GetBlock( x, y, z );
				Vector3 min = new Vector3( x, y, z ) + info.MinBB[block];
				Vector3 max = new Vector3( x, y, z ) + info.MaxBB[block];
				
				AABB blockBB = new AABB( min, max );
				if( !blockBB.Intersects( bounds ) ) continue;
				if( condition( block ) ) return true;
			}
			return false;
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity are rope. </summary>
		public bool TouchesAnyRope() {
			AABB bounds = CollisionBounds;
			bounds.Max.Y += 0.5f/16f;
			return TouchesAny( bounds, b => b == (byte)Block.Rope );
		}
		
		/// <summary> Constant offset used to avoid floating point roundoff errors. </summary>
		public const float Adjustment = 0.001f;
		
		
		static readonly Vector3 liqExpand = new Vector3( 0.25f/16f, 0/16f, 0.25f/16f );
		
		// If liquid block above, leave height same
		// otherwise reduce water BB height by 0.5 blocks
		bool TouchesAnyLiquid( AABB bounds, byte block1, byte block2 ) {
			Vector3I bbMin = Vector3I.Floor( bounds.Min );
			Vector3I bbMax = Vector3I.Floor( bounds.Max );
			int height = game.World.Height;
			
			// Order loops so that we minimise cache misses
			for( int y = bbMin.Y; y <= bbMax.Y; y++ )
				for( int z = bbMin.Z; z <= bbMax.Z; z++ )
					for( int x = bbMin.X; x <= bbMax.X; x++ )
			{
				if( !game.World.IsValidPos( x, y, z ) ) continue;
				byte block = game.World.GetBlock( x, y, z );
				byte below = (y - 1) < 0 ? (byte)0 : game.World.GetBlock( x, y - 1, z );
				byte above = (y + 1) >= height ? (byte)0 : game.World.GetBlock( x, y + 1, z );
				
				// TODO: use recording to find right constants when I have more time
				Vector3 min = new Vector3( x, y, z ) + info.MinBB[block];
				Vector3 max = new Vector3( x, y, z ) + info.MaxBB[block];
				//if( game.BlockInfo.Collide[below] != CollideType.SwimThrough )
				//	min.Y += 4/16f;
				//if( game.BlockInfo.Collide[above] != CollideType.SwimThrough )
				//	max.Y -= 4/16f;
				
				AABB blockBB = new AABB( min, max );
				if( !blockBB.Intersects( bounds ) ) continue;		
				if( block == block1 || block == block2 ) return true;
			}
			return false;
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity are lava or still lava. </summary>
		public bool TouchesAnyLava() {
			AABB bounds = CollisionBounds.Expand( liqExpand );
			return TouchesAnyLiquid( bounds, (byte)Block.Lava, (byte)Block.StillLava );
		}

		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity are water or still water. </summary>
		public bool TouchesAnyWater() {
			AABB bounds = CollisionBounds.Expand( liqExpand );
			return TouchesAnyLiquid( bounds, (byte)Block.Water, (byte)Block.StillWater );
		}
	}
}