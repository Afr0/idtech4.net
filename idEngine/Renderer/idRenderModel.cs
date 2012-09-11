﻿/*
===========================================================================

Doom 3 GPL Source Code
Copyright (C) 1999-2011 id Software LLC, a ZeniMax Media company. 

This file is part of the Doom 3 GPL Source Code (?Doom 3 Source Code?).  

Doom 3 Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Doom 3 Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Doom 3 Source Code.  If not, see <http://www.gnu.org/licenses/>.

In addition, the Doom 3 Source Code is also subject to certain additional terms. You should have received a copy of these additional terms immediately following the terms and conditions of the GNU General Public License which accompanied the Doom 3 Source Code.  If not, please request a copy in writing from id Software at the address below.

If you have questions concerning this license or the applicable additional terms, you may contact in writing id Software LLC, c/o ZeniMax Media Inc., Suite 120, Rockville, Maryland 20850 USA.

===========================================================================
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using idTech4.Geometry;

namespace idTech4.Renderer
{
	public abstract class idRenderModel : IDisposable
	{
		#region Properties
		/// <summary>
		/// Gets the default animation pose.
		/// </summary>
		public abstract idJointQuaternion[] DefaultPose
		{
			get;
		}

		/// <summary>
		/// Gets a value greater than 0.0 if the model requires the depth hack.
		/// </summary>
		public abstract float DepthHack
		{
			get;
		}

		/// <summary>
		/// If the load failed for any reason, this will return true.
		/// </summary>
		public abstract bool IsDefault
		{
			get;
		}

		public abstract DynamicModel IsDynamic
		{
			get;
		}

		public bool IsLevelLoadReferenced
		{
			get;
			set;
		}

		public abstract bool IsLoaded
		{
			get;
		}

		/// <summary>
		/// Models parsed from inside map files or dynamically created cannot be reloaded by reloadmodels.
		/// </summary>
		public abstract bool IsReloadable
		{
			get;
		}
			
		/// <summary>
		/// Models of the form "_area*" may have a prelight shadow model associated with it.
		/// </summary>
		public abstract bool IsStaticWorld
		{
			get;
		}

		/// <summary>
		/// Gets the number of joints or 0 if the model is not an MD5.
		/// </summary>
		public abstract int JointCount
		{
			get;
		}

		/// <summary>
		/// Gets the MD5 joints or NULL if the model is not an MD5.
		/// </summary>
		public abstract idMD5Joint[] Joints
		{
			get;
		}

		/// <summary>
		/// Reports the amount of memory (roughly) consumed by the model.
		/// </summary>
		/// <returns></returns>
		public abstract int MemoryUsage
		{
			get;
		}

		/// <summary>
		/// Gets the name of the model.
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// Gets the number of surfaces in the model.
		/// </summary>
		public abstract int SurfaceCount
		{
			get;
		}
		#endregion

		#region Constructor
		public idRenderModel()
		{

		}

		~idRenderModel()
		{
			Dispose(false);
		}
		#endregion

		#region Methods
		#region Public
		/// <summary>
		/// Dynamic model instantiations will be created with this.
		/// </summary>
		/// <remarks>
		/// the geometry data will be owned by the model, and freed when it is freed
		/// the geoemtry should be raw triangles, with no extra processing
		/// </remarks>
		/// <param name="surface"></param>
		public abstract void AddSurface(RenderModelSurface surface);

		/// <summary>
		/// Cleans all the geometry and performs cross-surface processing like shadow hulls.
		/// </summary>
		/// <remarks>
		/// Creates the duplicated back side geometry for two sided, alpha tested, lit materials
		/// This does not need to be called if none of the surfaces added with AddSurface require
		/// light interaction, and all the triangles are already well formed.
		/// </remarks>
		public abstract void FinishSurfaces();

		/// <summary>
		/// Dump any ambient caches on the model surfaces.
		/// </summary>
		public abstract void FreeVertexCache();

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Dynamic models should return a fast, conservative approximation.
		/// Static models should usually return the exact value.
		/// </remarks>
		/// <param name="renderEntity"></param>
		/// <returns></returns>
		public abstract idBounds GetBounds(RenderEntityComponent renderEntity = null);
		
		/// <summary>
		/// Gets the joint with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public abstract int GetJointIndex(string name);

		/// <summary>
		/// Gets the index of the joint with the given instance.
		/// </summary>
		/// <param name="joint"></param>
		/// <returns></returns>
		public abstract int GetJointIndex(idMD5Joint joint);

		/// <summary>
		/// Gets the name of the joint with the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public abstract string GetJointName(int index);

		public abstract int GetNearestJoint(int surfaceIndex, int a, int c, int b);

		public abstract RenderModelSurface GetSurface(int index);

		/// <summary>
		/// This is used for dynamically created surfaces, which are assumed to not be reloadable.
		/// It can be called again to clear out the surfaces of a dynamic model for regeneration.
		/// </summary>
		/// <param name="name"></param>
		public abstract void InitEmpty(string name);

		/// <summary>
		/// Loads static models only, dynamic models must be loaded by the modelManager.
		/// </summary>
		/// <param name="fileName"></param>
		public abstract void InitFromFile(string fileName);

		/// <summary>
		/// Creates a static model based on the definition and view currently.
		/// </summary>
		/// <remarks>
		/// This will be regenerated for every view, even though
		/// some models, like character meshes, could be used for multiple (mirror)
		/// views in a frame, or may stay static for multiple frames (corpses)
		/// The renderer will delete the returned dynamic model the next view.
		/// </remarks>
		/// <param name="renderEntity"></param>
		/// <param name="view"></param>
		/// <param name="cachedModel"></param>
		/// <returns></returns>
		public abstract idRenderModel InstantiateDynamicModel(idRenderEntity renderEntity, View view, idRenderModel cachedModel);

		/// <summary>
		/// Prints a single line report for listModels.
		/// </summary>
		public abstract void List();

		/// <summary>
		/// Used for initial loads, reloadModel, and reloading the data of purged models.
		/// </summary>
		/// <remarks>
		/// Upon exit, the model will absolutely be valid, but possibly as a default model.
		/// </remarks>
		public abstract void Load();

		public abstract void MakeDefault();

		/// <summary>
		/// renderBump uses this to load the very high poly count models, skipping the
		/// shadow and tangent generation, along with some surface cleanup to make it load faster.
		/// </summary>
		/// <param name="fileName"></param>
		public abstract void PartialInitFromFile(string fileName);

		/// <summary>
		/// Prints a detailed report on the model for printModel.
		/// </summary>
		public abstract void Print();

		/// <summary>
		/// Frees all the data, but leaves the class around for dangling references, which can regenerate the data with LoadModel().
		/// </summary>
		public abstract void Purge();

		/// <summary>
		/// Resets any model information that needs to be reset on a same level load etc.. 
		/// currently only implemented for liquids.
		/// </summary>
		public abstract void Reset();
				
		/// <summary>
		/// Models that are already loaded at level start time will still touch their data 
		/// to make sure they are kept loaded.
		/// </summary>
		public abstract void TouchData();


		/* TODO:
	// for reloadModels
	virtual ID_TIME_T				Timestamp() const = 0;



	// NumBaseSurfaces will not count any overlays added to dynamic models
	virtual int					NumBaseSurfaces() const = 0;



	// Allocates surface triangles.
	// Allocates memory for srfTriangles_t::verts and srfTriangles_t::indexes
	// The allocated memory is not initialized.
	// srfTriangles_t::numVerts and srfTriangles_t::numIndexes are set to zero.
	virtual srfTriangles_t *	AllocSurfaceTriangles( int numVerts, int numIndexes ) const = 0;

	// Frees surfaces triangles.
	virtual void				FreeSurfaceTriangles( srfTriangles_t *tris ) const = 0;

	// created at load time by stitching together all surfaces and sharing
	// the maximum number of edges.  This may be incorrect if a skin file
	// remaps surfaces between shadow casting and non-shadow casting, or
	// if some surfaces are noSelfShadow and others aren't
	virtual srfTriangles_t	*	ShadowHull() const = 0;
		
	// models parsed from inside map files or dynamically created cannot be reloaded by
	// reloadmodels
	virtual bool				IsReloadable() const = 0;
		
	

	// Writing to and reading from a demo file.
	virtual void				ReadFromDemoFile( class idDemoFile *f ) = 0;
	virtual void				WriteToDemoFile( class idDemoFile *f ) = 0;
};*/
		#endregion
		#endregion

		#region IDisposable implementation
		#region Properties
		public bool Disposed
		{
			get
			{
				return _disposed;
			}
		}
		#endregion

		#region Members
		private bool _disposed;
		#endregion

		#region Methods
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(this.Disposed == true)
			{
				throw new ObjectDisposedException(this.GetType().Name);
			}

			if(disposing == true)
			{
				Purge();
			}

			_disposed = true;
		}
		#endregion
		#endregion
	}

	public enum DynamicModel
	{
		/// <summary>
		/// Never creates a dynamic model.
		/// </summary>
		Static,

		/// <summary>
		/// Once created, stays constant until the entity is updated (animating characters).
		/// </summary>
		Cached,

		/// <summary>
		/// Must be recreated for every single view (time dependent things like particles).
		/// </summary>
		Continuous
	}
}