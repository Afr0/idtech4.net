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
using System.Diagnostics;
using System.Linq;
using System.Text;

using idTech4.Sound;
using idTech4.Text;
using idTech4.Text.Decl;

namespace idTech4.Renderer
{
	/// <summary>
	/// Material.
	/// </summary>
	/// <remarks>
	/// Any errors during parsing just set MF_DEFAULTED and return, rather than throwing
	/// a hard error. This will cause the material to fall back to default material,
	/// but otherwise let things continue.
	/// <p/>
	/// Each material may have a set of calculations that must be evaluated before
	/// drawing with it.
	/// <p/>
	/// Every expression that a material uses can be evaluated at one time, which
	/// will allow for perfect common subexpression removal when I get around to
	/// writing it.
	/// <p/>
	/// Without this, scrolling an entire surface could result in evaluating the
	/// same texture matrix calculations a half dozen times.
	/// <p/>
	/// Open question: should I allow arbitrary per-vertex color, texCoord, and vertex
	/// calculations to be specified in the material code?
	/// <p/>
	/// Every stage will definately have a valid image pointer.
	/// <p/>
	/// We might want the ability to change the sort value based on conditionals,
	/// but it could be a hassle to implement,
	/// </remarks>
	public sealed class idMaterial : idDecl
	{
		#region Constants
		private const int TopPriority = 4;
		private const int PredefinedRegisterCount = 21;
		private readonly MaterialInfoParameter[] InfoParameters = new MaterialInfoParameter[] {
			// game relevant attributes
			new MaterialInfoParameter("solid",			false, SurfaceFlags.None, ContentFlags.Solid),		// may need to override a clearSolid
			new MaterialInfoParameter("water",			true,	SurfaceFlags.None, ContentFlags.Water),		// used for water
			new MaterialInfoParameter("playerclip",		false,	SurfaceFlags.None, ContentFlags.PlayerClip ),	// solid to players
			new MaterialInfoParameter("monsterclip",	false,	SurfaceFlags.None, ContentFlags.MonsterClip),	// solid to monsters
			new MaterialInfoParameter("moveableclip",	false,	SurfaceFlags.None, ContentFlags.MoveableClip),// solid to moveable entities
			new MaterialInfoParameter("ikclip",			false,	SurfaceFlags.None,	ContentFlags.IkClip),		// solid to IK
			new MaterialInfoParameter("blood",			false,	SurfaceFlags.None,	ContentFlags.Blood),		// used to detect blood decals
			new MaterialInfoParameter("trigger",		false,	SurfaceFlags.None, ContentFlags.Trigger),		// used for triggers
			new MaterialInfoParameter("aassolid",		false,	SurfaceFlags.None,	ContentFlags.AasSolid),	// solid for AAS
			new MaterialInfoParameter("aasobstacle",	false,	SurfaceFlags.None, ContentFlags.AasObstacle),// used to compile an obstacle into AAS that can be enabled/disabled
			new MaterialInfoParameter("flashlight_trigger",	false,	SurfaceFlags.None,	ContentFlags.FlashlightTrigger), // used for triggers that are activated by the flashlight
			new MaterialInfoParameter("nonsolid",		true,	SurfaceFlags.None,	ContentFlags.None ),					// clears the solid flag
			new MaterialInfoParameter("nullNormal",		false,	SurfaceFlags.NullNormal, ContentFlags.None ),		// renderbump will draw as 0x80 0x80 0x80

			// utility relevant attributes
			new MaterialInfoParameter("areaportal",		true,	SurfaceFlags.None,	ContentFlags.AreaPortal),	// divides areas
			new MaterialInfoParameter("qer_nocarve",	true,	SurfaceFlags.None, ContentFlags.NoCsg),		// don't cut brushes in editor

			new MaterialInfoParameter("discrete",		true,	SurfaceFlags.Discrete,	ContentFlags.None ),		// surfaces should not be automatically merged together or
																													// clipped to the world,
																													// because they represent discrete objects like gui shaders
																													// mirrors, or autosprites
			new MaterialInfoParameter("noFragment",		false,	SurfaceFlags.NoFragment,	ContentFlags.None ),

			new MaterialInfoParameter("slick",			false,	SurfaceFlags.Slick,		ContentFlags.None ),
			new MaterialInfoParameter("collision",		false,	SurfaceFlags.Collision,	ContentFlags.None ),
			new MaterialInfoParameter("noimpact",		false,	SurfaceFlags.NoImpact,	ContentFlags.None ),		// don't make impact explosions or marks
			new MaterialInfoParameter("nodamage",		false,	SurfaceFlags.NoDamage,	ContentFlags.None ),		// no falling damage when hitting
			new MaterialInfoParameter("ladder",			false,	SurfaceFlags.Ladder,	ContentFlags.None ),		// climbable
			new MaterialInfoParameter("nosteps",		false,	SurfaceFlags.NoSteps,	ContentFlags.None ),		// no footsteps

			// material types for particle, sound, footstep feedback
			new MaterialInfoParameter("metal",			false,  SurfaceFlags.Metal,		ContentFlags.None ),	// metal
			new MaterialInfoParameter("stone",			false,  SurfaceFlags.Stone,		ContentFlags.None ),	// stone
			new MaterialInfoParameter("flesh",			false,  SurfaceFlags.Flesh,		ContentFlags.None ),	// flesh
			new MaterialInfoParameter("wood",			false,  SurfaceFlags.Wood,		ContentFlags.None ),	// wood
			new MaterialInfoParameter("cardboard",		false,	SurfaceFlags.Cardboard,	ContentFlags.None ),	// cardboard
			new MaterialInfoParameter("liquid",			false,	SurfaceFlags.Liquid,	ContentFlags.None ),	// liquid
			new MaterialInfoParameter("glass",			false,	SurfaceFlags.Glass,		ContentFlags.None ),	// glass
			new MaterialInfoParameter("plastic",		false,	SurfaceFlags.Plastic,	ContentFlags.None ),	// plastic
			new MaterialInfoParameter("ricochet",		false,	SurfaceFlags.Ricochet,	ContentFlags.None ),	// behaves like metal but causes a ricochet sound

			// unassigned surface types
			new MaterialInfoParameter("surftype10",		false,	SurfaceFlags.T10,	ContentFlags.None ),
			new MaterialInfoParameter("surftype11",		false,	SurfaceFlags.T11,	ContentFlags.None ),
			new MaterialInfoParameter("surftype12",		false,	SurfaceFlags.T12,	ContentFlags.None ),
			new MaterialInfoParameter("surftype13",		false,	SurfaceFlags.T13,	ContentFlags.None ),
			new MaterialInfoParameter("surftype14",		false,	SurfaceFlags.T14,	ContentFlags.None ),
			new MaterialInfoParameter("surftype15",		false,	SurfaceFlags.T15,	ContentFlags.None )
		};
		#endregion

		#region Properties
		public float[] ConstantRegisters
		{
			get
			{
				return _constantRegisters;
			}
		}

		/// <summary>
		/// For interaction list linking and dmap flood filling.  The depth buffer will not be filled for translucent surfaces.
		/// </summary>
		public MaterialCoverage Coverage
		{
			get
			{
				return _coverage;
			}
		}

		public CullType CullType
		{
			get
			{
				return _cullType;
			}
		}

		/// <summary>
		/// true if the material will draw any non light interaction stages.
		/// </summary>
		public bool HasAmbient
		{
			get
			{
				return (_ambientStageCount > 0);
			}
		}

		/// <summary>
		/// Just for image resource tracking.
		/// </summary>
		public int ImageClassification
		{
			set
			{
				idImage image;

				foreach(MaterialStage stage in _stages)
				{
					image = stage.Texture.Image;

					if(image != null)
					{
						image.Classification = value;
					}
				}
			}
		}

		public int ImageHeight
		{
			get
			{
				return GetStage(0).Texture.Image.Height;
			}
		}

		public int ImageWidth
		{
			get
			{
				return GetStage(0).Texture.Image.Width;
			}
		}	

		/// <summary>
		/// Returns true if the material will draw anything at all.
		/// </summary>
		/// <remarks>
		/// Triggers, portals, etc., will not have anything to draw.  A not drawn surface 
		/// can still castShadow, which can be used to make a simplified shadow hull for a 
		/// complex object set as noShadow.
		/// </remarks>
		public bool IsDrawn
		{
			get
			{
				return ((_stageCount > 0)/* TODO: || (_entityGui != 0) || (_gui != Nullable)*/);
			}
		}

		public bool IsPortalSky
		{
			get
			{
				return _portalSky;
			}
		}

		/// <summary>
		/// Gets or sets material specific flags.
		/// </summary>		
		public MaterialFlags MaterialFlag
		{
			get
			{
				return _materialFlags;
			}
			set
			{
				_materialFlags |= value;
			}
		}

		public float PolygonOffset
		{
			get
			{
				return _polygonOffset;
			}
		}

		public int RegisterCount
		{
			get
			{
				return _registerCount;
			}
		}

		/// <summary>
		/// This is only used by the gui system to force sorting order
		/// on images referenced from tga's instead of materials. 
		/// this is done this way as there are 2000 tgas the guis use
		/// </summary>
		public float Sort
		{
			get
			{
				return (float) _sort;
			}
			set
			{
				_sort = value;
			}
		}

		public MaterialStage[] Stages
		{
			get
			{
				return _stages;
			}
		}

		/// <summary>
		/// Gets the surface flags.
		/// </summary>
		public SurfaceFlags SurfaceFlags
		{
			get
			{
				return _surfaceFlags;
			}
		}
		#endregion

		#region Members
		private string _description;			// description
		private string _renderBump;				// renderbump command options, without the "renderbump" at the start

		private ContentFlags _contentFlags;
		private SurfaceFlags _surfaceFlags;
		private MaterialFlags _materialFlags;
		private CullType _cullType;

		private DeformType _deformType;
		private idDecl _deformDecl;				// for surface emitted particle deforms and tables
		private int[] _deformRegisters;			// numeric parameter for deforms

		private DecalInfo _decalInfo;
		private int[] _texGenRegisters;

		private MaterialCoverage _coverage;
		private float _sort;					// lower numbered shaders draw before higher numbered
		private bool _shouldCreateBackSides;

		private idImage _lightFalloffImage;

		// we defer loading of the editor image until it is asked for, so the game doesn't load up all the invisible and uncompressed images
		// If editorImage is NULL, it will atempt to load editorImageName, and set editorImage to that or defaultImage
		private string _editorImageName;

		private bool _suppressInSubview;
		private bool _portalSky;
		private bool _fogLight;
		private bool _blendLight;
		private bool _ambientLight;
		private bool _unsmoothedTangents;
		private bool _hasSubview;
		private bool _allowOverlays;
		private bool _noFog;					// surface does not create fog interactions

		private int _stageCount;
		private int _registerCount;
		private int _ambientStageCount;

		private float _polygonOffset;
		private int _spectrum;					// for invisible writing, used for both lights and surfaces

		private MaterialStage[] _stages;
		private ExpressionOperation[] _ops;		// evaluate to make _expressionRegisters
		private float[] _expressionRegisters;
		private float[] _constantRegisters;		// null if _ops ever reference globalParms or entityParms

		private MaterialParsingData _parsingData;
		#endregion

		#region Constructor
		public idMaterial()
		{
			Clear();
		}
		#endregion

		#region Methods
		#region Public
		public void EvaluateRegisters(ref float[] registers, float[] shaderParms, View view)
		{
			EvaluateRegisters(ref registers, shaderParms, view, null);
		}

		public void EvaluateRegisters(ref float[] registers, float[] shaderParms, View view, idSoundEmitter soundEmitter)
		{
			// copy the material constants.
			for(int i = PredefinedRegisterCount; i < _registerCount; i++)
			{
				registers[i] = _expressionRegisters[i];
			}

			// copy the local and global parameters.
			registers[(int) ExpressionRegister.Time] = view.FloatTime;
			registers[(int) ExpressionRegister.Parm0] = shaderParms[0];
			registers[(int) ExpressionRegister.Parm1] = shaderParms[1];
			registers[(int) ExpressionRegister.Parm2] = shaderParms[2];
			registers[(int) ExpressionRegister.Parm3] = shaderParms[3];
			registers[(int) ExpressionRegister.Parm4] = shaderParms[4];
			registers[(int) ExpressionRegister.Parm5] = shaderParms[5];
			registers[(int) ExpressionRegister.Parm6] = shaderParms[6];
			registers[(int) ExpressionRegister.Parm7] = shaderParms[7];
			registers[(int) ExpressionRegister.Parm8] = shaderParms[8];
			registers[(int) ExpressionRegister.Parm9] = shaderParms[9];
			registers[(int) ExpressionRegister.Parm10] = shaderParms[10];
			registers[(int) ExpressionRegister.Parm11] = shaderParms[11];

			if(view.RenderView.MaterialParameters != null)
			{
				registers[(int) ExpressionRegister.Global0] = view.RenderView.MaterialParameters[0];
				registers[(int) ExpressionRegister.Global1] = view.RenderView.MaterialParameters[1];
				registers[(int) ExpressionRegister.Global2] = view.RenderView.MaterialParameters[2];
				registers[(int) ExpressionRegister.Global3] = view.RenderView.MaterialParameters[3];
				registers[(int) ExpressionRegister.Global4] = view.RenderView.MaterialParameters[4];
				registers[(int) ExpressionRegister.Global5] = view.RenderView.MaterialParameters[5];
				registers[(int) ExpressionRegister.Global6] = view.RenderView.MaterialParameters[6];
				registers[(int) ExpressionRegister.Global7] = view.RenderView.MaterialParameters[7];
			}

			ExpressionOperation op;
			int b;
			int opCount = (_ops != null) ? _ops.Length : 0;

			for(int i = 0; i < opCount; i++)
			{
				op = _ops[i];

				switch(op.OperationType)
				{
					case ExpressionOperationType.Add:
						registers[op.C] = registers[op.A] + registers[op.B];
						break;

					case ExpressionOperationType.Subtract:
						registers[op.C] = registers[op.A] - registers[op.B];
						break;

					case ExpressionOperationType.Multiply:
						registers[op.C] = registers[op.A] * registers[op.B];
						break;

					case ExpressionOperationType.Divide:
						registers[op.C] = registers[op.A] / registers[op.B];
						break;

					case ExpressionOperationType.Modulo:
						b = (int) registers[op.B];
						b = (b != 0) ? b : 1;

						registers[op.C] = (int) registers[op.A] % b;
						break;

					case ExpressionOperationType.Table:
						idDeclTable table = (idDeclTable) idE.DeclManager.DeclByIndex(DeclType.Table, op.A);
						registers[op.C] = table.Lookup(registers[op.B]);
						break;
					case ExpressionOperationType.Sound:
						idConsole.Warning("TODO: eoptype sound");
						// TODO: OP_TYPE_SOUND:
						/*if ( soundEmitter ) {
							registers[op->c] = soundEmitter->CurrentAmplitude();
						} else {*/
						registers[op.C] = 0;
						//}
						break;
					case ExpressionOperationType.GreaterThan:
						registers[op.C] = ((registers[op.A] > registers[op.B]) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.GreaterThanOrEquals:
						registers[op.C] = ((registers[op.A] >= registers[op.B]) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.LessThan:
						registers[op.C] = ((registers[op.A] < registers[op.B]) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.LessThanOrEquals:
						registers[op.C] = ((registers[op.A] <= registers[op.B]) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.Equals:
						registers[op.C] = ((registers[op.A] == registers[op.B]) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.NotEquals:
						registers[op.C] = ((registers[op.A] != registers[op.B]) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.And:
						registers[op.C] = (((registers[op.A] != 0) && (registers[op.B] != 0)) == true) ? 1 : 0;
						break;
					case ExpressionOperationType.Or:
						registers[op.C] = (((registers[op.A] != 0) || (registers[op.B] != 0)) == true) ? 1 : 0;
						break;
				}
			}
		}

		public MaterialStage GetStage(int index)
		{
			if((index < 0) || (index > _stages.Length))
			{
				throw new IndexOutOfRangeException();
			}

			return _stages[index];
		}

		/// <summary>
		/// Test for existance of specific material flag(s).
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public bool TestMaterialFlag(MaterialFlags flag)
		{
			return ((_materialFlags & flag) != 0);
		}
		#endregion

		#region Private
		private void Clear()
		{
			_description = "<none>";
			_renderBump = string.Empty;

			_contentFlags = ContentFlags.Solid;
			_surfaceFlags = SurfaceFlags.None;
			_materialFlags = 0;

			_sort = (float) MaterialSort.Bad;
			_coverage = MaterialCoverage.Bad;
			_cullType = CullType.Front;

			_deformType = DeformType.None;
			_deformRegisters = new int[4];

			_ops = null;
			_expressionRegisters = null;
			_constantRegisters = null;
			_stages = new MaterialStage[] { };

			_stageCount = 0;
			_ambientStageCount = 0;
			_registerCount = 0;


			_lightFalloffImage = null;
			/*entityGui = 0;*/
			_shouldCreateBackSides = false;
			_editorImageName = null;

			_fogLight = false;
			_blendLight = false;
			_ambientLight = false;
			_noFog = false;
			_hasSubview = false;
			_allowOverlays = true;
			_unsmoothedTangents = false;

			/*gui = NULL;
			
			editorAlpha = 1.0;*/
			_spectrum = 0;
			/* refCount = 0;*/

			_polygonOffset = 0;
			_suppressInSubview = false;
			_portalSky = false;

			_decalInfo.StayTime = 10000;
			_decalInfo.FadeTime = 4000;
			_decalInfo.Start = new float[] { 1, 1, 1, 1 };
			_decalInfo.End = new float[] { 0, 0, 0, 0 };
		}

		/// <summary>
		/// Parses the material, if there are any errors during parsing the defaultShader will be set.
		/// </summary>
		/// <param name="lexer"></param>
		private void ParseMaterial(idLexer lexer)
		{
			int s = 0;

			_registerCount = PredefinedRegisterCount; // leave space for the parms to be copied in.

			for(int i = 0; i < _registerCount; i++)
			{
				_parsingData.RegisterIsTemporary[i] = true; // they aren't constants that can be folded.
			}

			TextureRepeat textureRepeatDefault = TextureRepeat.Repeat; // allow a global setting for repeat.
			idToken token = null;

			string tokenValue;
			string tokenLower;

			while(true)
			{
				if(TestMaterialFlag(Renderer.MaterialFlags.Defaulted) == true)
				{
					// we have a parse error.
					return;
				}

				if((token = lexer.ExpectAnyToken()) == null)
				{
					this.MaterialFlag = MaterialFlags.Defaulted;
					return;
				}

				tokenValue = token.ToString();
				tokenLower = tokenValue.ToLower();

				// end of material definition
				if(tokenLower == "}")
				{
					break;
				}
				else if(tokenLower == "qer_editorimage")
				{
					_editorImageName = lexer.ReadTokenOnLine().ToString();
					lexer.SkipRestOfLine();
				}
				else if(tokenLower == "description")
				{
					_description = lexer.ReadTokenOnLine().ToString();
				}
				// check for the surface / content bit flags.
				else if(CheckSurfaceParameter(token) == true)
				{

				}
				else if(tokenLower == "polygonoffset")
				{
					this.MaterialFlag = Renderer.MaterialFlags.PolygonOffset;

					if((token = lexer.ReadTokenOnLine()) == null)
					{
						_polygonOffset = 1;
					}
					else
					{
						_polygonOffset = token.ToFloat();
					}
				}
				// noshadow.
				else if(tokenLower == "noshadows")
				{
					this.MaterialFlag = MaterialFlags.NoShadows;
				}
				else if(tokenLower == "suppressinsubview")
				{
					_suppressInSubview = true;
				}
				else if(tokenLower == "portalsky")
				{
					_portalSky = true;
				}
				else if(tokenLower == "noselfshadow")
				{
					this.MaterialFlag = Renderer.MaterialFlags.NoSelfShadow;
				}
				else if(tokenLower == "noportalfog")
				{
					this.MaterialFlag = Renderer.MaterialFlags.NoPortalFog;
				}
				// forceShadows allows nodraw surfaces to cast shadows.
				else if(tokenLower == "forceshadows")
				{
					this.MaterialFlag = Renderer.MaterialFlags.ForceShadows;
				}
				// overlay / decal suppression.
				else if(tokenLower == "nooverlays")
				{
					_allowOverlays = false;
				}
				// moster blood overlay forcing for alpha tested or translucent surfaces.
				else if(tokenLower == "forceoverlays")
				{
					_parsingData.ForceOverlays = true;
				}
				else if(tokenLower == "translucent")
				{
					_coverage = MaterialCoverage.Translucent;
				}
				// global zero clamp.
				else if(tokenLower == "zeroclamp")
				{
					textureRepeatDefault = TextureRepeat.ClampToZero;
				}
				// global clamp.
				else if(tokenLower == "clamp")
				{
					textureRepeatDefault = TextureRepeat.Clamp;
				}
				// global clamp.
				else if(tokenLower == "alphazeroclamp")
				{
					textureRepeatDefault = TextureRepeat.ClampToZero;
				}
				// forceOpaque is used for skies-behind-windows.
				else if(tokenLower == "forceopaque")
				{
					_coverage = MaterialCoverage.Opaque;
				}
				else if(tokenLower == "twosided")
				{
					_cullType = CullType.TwoSided;

					// twoSided implies no-shadows, because the shadow
					// volume would be coplanar with the surface, giving depth fighting
					// we could make this no-self-shadows, but it may be more important
					// to receive shadows from no-self-shadow monsters.
					this.MaterialFlag = Renderer.MaterialFlags.NoShadows;
				}
				else if(tokenLower == "backsided")
				{
					_cullType = CullType.Back;

					// the shadow code doesn't handle this, so just disable shadows.
					// We could fix this in the future if there was a need.
					this.MaterialFlag = Renderer.MaterialFlags.NoShadows;
				}
				else if(tokenLower == "foglight")
				{
					_fogLight = true;
				}
				else if(tokenLower == "blendlight")
				{
					_blendLight = true;
				}
				else if(tokenLower == "ambientlight")
				{
					_ambientLight = true;
				}
				else if(tokenLower == "mirror")
				{
					_sort = (float) MaterialSort.Subview;
					_coverage = MaterialCoverage.Opaque;
				}
				else if(tokenLower == "nofog")
				{
					_noFog = true;
				}
				else if(tokenLower == "unsmoothedtangents")
				{
					_unsmoothedTangents = true;
				}
				// lightFallofImage <imageprogram>
				// specifies the image to use for the third axis of projected
				// light volumes.
				else if(tokenLower == "lightfalloffimage")
				{
					_lightFalloffImage = idE.ImageManager.ImageFromFile(ParsePastImageProgram(lexer), TextureFilter.Default, false, TextureRepeat.Clamp, TextureDepth.Default);
				}
				// guisurf <guifile> | guisurf entity
				// an entity guisurf must have an idUserInterface
				// specified in the renderEntity.
				else if(tokenLower == "guisurf")
				{
					idConsole.Warning("TODO: idMaterial keyword guiSurf");
					token = lexer.ReadTokenOnLine();

					/*if ( !token.Icmp( "entity" ) ) {
						entityGui = 1;
					} else if ( !token.Icmp( "entity2" ) ) {
						entityGui = 2;
					} else if ( !token.Icmp( "entity3" ) ) {
						entityGui = 3;
					} else {
						gui = uiManager->FindGui( token.c_str(), true );
					}*/
				}
				// sort.
				else if(tokenLower == "sort")
				{
					ParseSort(lexer);
				}
				// spectrum <integer>.
				else if(tokenLower == "spectrum")
				{
					int.TryParse(lexer.ReadTokenOnLine().ToString(), out _spectrum);
				}
				// deform < sprite | tube | flare >.
				else if(tokenLower == "deform")
				{
					ParseDeform(lexer);
				}
				// decalInfo <staySeconds> <fadeSeconds> (<start rgb>) (<end rgb>).
				else if(tokenLower == "decalinfo")
				{
					ParseDecalInfo(lexer);
				}
				// renderbump <args...>.
				else if(tokenLower == "renderbump")
				{
					_renderBump = lexer.ParseRestOfLine();
				}
				// diffusemap for stage shortcut.
				else if(tokenLower == "diffusemap")
				{
					idConsole.Warning("TODO: idMaterial keyword diffuseMap");
					/*str = R_ParsePastImageProgram( src );
					idStr::snPrintf( buffer, sizeof( buffer ), "blend diffusemap\nmap %s\n}\n", str );
					newSrc.LoadMemory( buffer, strlen(buffer), "diffusemap" );
					newSrc.SetFlags( LEXFL_NOFATALERRORS | LEXFL_NOSTRINGCONCAT | LEXFL_NOSTRINGESCAPECHARS | LEXFL_ALLOWPATHNAMES );
					ParseStage( newSrc, trpDefault );
					newSrc.FreeSource();*/
				}
				// specularmap for stage shortcut.
				else if(tokenLower == "specularmap")
				{
					idConsole.Warning("TODO: idMaterial keyword specularMap");
					/*str = R_ParsePastImageProgram( src );
					idStr::snPrintf( buffer, sizeof( buffer ), "blend specularmap\nmap %s\n}\n", str );
					newSrc.LoadMemory( buffer, strlen(buffer), "specularmap" );
					newSrc.SetFlags( LEXFL_NOFATALERRORS | LEXFL_NOSTRINGCONCAT | LEXFL_NOSTRINGESCAPECHARS | LEXFL_ALLOWPATHNAMES );
					ParseStage( newSrc, trpDefault );
					newSrc.FreeSource();*/
				}
				// normalmap for stage shortcut.
				else if(tokenLower == "bumpmap")
				{
					idConsole.Warning("TODO: idMaterial keyword bumpMap");
					/*str = R_ParsePastImageProgram( src );
					idStr::snPrintf( buffer, sizeof( buffer ), "blend bumpmap\nmap %s\n}\n", str );
					newSrc.LoadMemory( buffer, strlen(buffer), "bumpmap" );
					newSrc.SetFlags( LEXFL_NOFATALERRORS | LEXFL_NOSTRINGCONCAT | LEXFL_NOSTRINGESCAPECHARS | LEXFL_ALLOWPATHNAMES );
					ParseStage( newSrc, trpDefault );
					newSrc.FreeSource();*/
				}
				// DECAL_MACRO for backwards compatibility with the preprocessor macros.
				else if(tokenLower == "decal_macro")
				{
					// polygonOffset
					this.MaterialFlag = Renderer.MaterialFlags.PolygonOffset;
					_polygonOffset = -1;

					// discrete
					_surfaceFlags |= SurfaceFlags.Discrete;
					_contentFlags &= ~ContentFlags.Solid;

					// sort decal.
					_sort = (float) MaterialSort.Decal;

					// noShadows
					this.MaterialFlag = Renderer.MaterialFlags.NoShadows;
				}
				else if(tokenValue == "{")
				{
					// create the new stage.
					ParseStage(lexer, textureRepeatDefault);
				}
				else
				{
					idConsole.WriteLine("unknown general material parameter '{0}' in '{1}'", tokenValue, this.Name);
					return;
				}
			}

			// add _flat or _white stages if needed.
			AddImplicitStages();

			// order the diffuse / bump / specular stages properly.
			SortInteractionStages();

			// if we need to do anything with normals (lighting or environment mapping)
			// and two sided lighting was asked for, flag
			// shouldCreateBackSides() and change culling back to single sided,
			// so we get proper tangent vectors on both sides.

			// we can't just call ReceivesLighting(), because the stages are still
			// in temporary form.
			if(_cullType == CullType.TwoSided)
			{
				for(int i = 0; i < _parsingData.Stages.Count; i++)
				{
					if((_parsingData.Stages[i].Lighting != StageLighting.Ambient) || (_parsingData.Stages[i].Texture.TextureCoordinates != TextureCoordinateGeneration.Explicit))
					{
						if(_cullType == CullType.TwoSided)
						{
							_cullType = CullType.Front;
							_shouldCreateBackSides = true;
						}

						break;
					}
				}
			}

			// currently a surface can only have one unique texgen for all the stages on old hardware.
			TextureCoordinateGeneration firstGen = TextureCoordinateGeneration.Explicit;

			for(int i = 0; i < _parsingData.Stages.Count; i++)
			{
				if(_parsingData.Stages[i].Texture.TextureCoordinates != TextureCoordinateGeneration.Explicit)
				{
					if(firstGen == TextureCoordinateGeneration.Explicit)
					{
						firstGen = _parsingData.Stages[i].Texture.TextureCoordinates;
					}
					else if(firstGen != _parsingData.Stages[i].Texture.TextureCoordinates)
					{
						idConsole.Warning("material '{0}' has multiple stages with a texgen", this.Name);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Adds implicit stages to the material.
		/// </summary>
		/// <remarks>
		/// If a material has diffuse or specular stages without any
		/// bump stage, add an implicit _flat bumpmap stage.
		/// <p/>
		/// It is valid to have either a diffuse or specular without the other.
		/// <p/>
		/// It is valid to have a reflection map and a bump map for bumpy reflection.
		/// </remarks>
		/// <param name="textureRepeatDefault"></param>
		private void AddImplicitStages(TextureRepeat textureRepeatDefault = TextureRepeat.Repeat)
		{
			bool hasDiffuse = false;
			bool hasSpecular = false;
			bool hasBump = false;
			bool hasReflection = false;

			for(int i = 0; i < _parsingData.Stages.Count; i++)
			{
				switch(_parsingData.Stages[i].Lighting)
				{
					case StageLighting.Bump:
						hasBump = true;
						break;

					case StageLighting.Diffuse:
						hasDiffuse = true;
						break;

					case StageLighting.Specular:
						hasSpecular = true;
						break;
				}

				if(_parsingData.Stages[i].Texture.TextureCoordinates == TextureCoordinateGeneration.ReflectCube)
				{
					hasReflection = true;
				}
			}

			// if it doesn't have an interaction at all, don't add anything
			if((hasBump == false) && (hasDiffuse == false) && (hasSpecular == false))
			{
				return;
			}

			idLexer lexer = new idLexer(LexerOptions.NoFatalErrors | LexerOptions.NoStringConcatination | LexerOptions.NoStringEscapeCharacters | LexerOptions.AllowPathNames);

			if(hasBump == false)
			{
				string bump = "blend bumpmap\nmap _flat\n}\n";

				lexer.LoadMemory(bump, "bumpmap");
				ParseStage(lexer, textureRepeatDefault);
			}

			if((hasDiffuse == false) && (hasSpecular == false) && (hasReflection == false))
			{
				string bump = "blend bumpmap\nmap _white\n}\n";

				lexer.LoadMemory(bump, "diffusemap");
				ParseStage(lexer, textureRepeatDefault);
			}
		}

		/// <summary>
		/// Sorts the shader stages.
		/// </summary>
		/// <remarks>
		/// The renderer expects bump, then diffuse, then specular
		/// There can be multiple bump maps, followed by additional
		/// diffuse and specular stages, which allows cross-faded bump mapping.
		/// <para/>
		/// Ambient stages can be interspersed anywhere, but they are
		/// ignored during interactions, and all the interaction
		/// stages are ignored during ambient drawing.
		/// </remarks>
		private void SortInteractionStages()
		{
			int i = 0, j = 0;

			for(i = 0; i < _parsingData.Stages.Count; i = j)
			{
				// find the next bump map
				for(j = i + 1; j < _parsingData.Stages.Count; j++)
				{
					if(_parsingData.Stages[j].Lighting == StageLighting.Bump)
					{
						// if the very first stage wasn't a bumpmap,
						// this bumpmap is part of the first group.
						if(_parsingData.Stages[i].Lighting != StageLighting.Bump)
						{
							continue;
						}

						break;
					}
				}
			}

			// bubble sort everything bump / diffuse / specular.
			for(int l = 1; l < j - i; l++)
			{
				for(int k = i; k < k - l; k++)
				{
					if(_parsingData.Stages[k].Lighting > _parsingData.Stages[k + 1].Lighting)
					{
						MaterialStage temp = _parsingData.Stages[k];

						_parsingData.Stages[k] = _parsingData.Stages[k + 1];
						_parsingData.Stages[k + 1] = temp;
					}
				}
			}
		}

		private void ParseSort(idLexer lexer)
		{
			idToken token = lexer.ReadTokenOnLine();

			if(token == null)
			{
				lexer.Warning("missing sort parameter");
				this.MaterialFlag = MaterialFlags.Defaulted;

				return;
			}

			try
			{
				_sort = (float) Enum.Parse(typeof(MaterialSort), token.ToString(), true);
			}
			catch(Exception x)
			{
				idConsole.Write(x.ToString());
				float.TryParse(token.ToString(), out _sort);
			}
		}

		private void ParseDeform(idLexer lexer)
		{
			idToken token = lexer.ExpectAnyToken();

			if(token == null)
			{
				return;
			}

			string tokenValue = token.ToString();
			string tokenLower = tokenValue.ToLower();

			if(tokenLower == "sprite")
			{
				_deformType = DeformType.Sprite;
				_cullType = CullType.TwoSided;

				this.MaterialFlag = MaterialFlags.NoShadows;
			}
			else if(tokenLower == "tube")
			{
				_deformType = DeformType.Tube;
				_cullType = CullType.TwoSided;

				this.MaterialFlag = MaterialFlags.NoShadows;
			}
			else if(tokenLower == "flare")
			{
				_deformType = DeformType.Flare;
				_cullType = CullType.TwoSided;
				_deformRegisters[0] = ParseExpression(lexer);

				this.MaterialFlag = MaterialFlags.NoShadows;
			}
			else if(tokenLower == "expand")
			{
				_deformType = DeformType.Expand;
				_deformRegisters[0] = ParseExpression(lexer);
			}
			else if(tokenLower == "move")
			{
				_deformType = DeformType.Move;
				_deformRegisters[0] = ParseExpression(lexer);
			}
			else if(tokenLower == "turbulent")
			{
				_deformType = DeformType.Turbulent;

				if((token = lexer.ExpectAnyToken()) == null)
				{
					lexer.Warning("deform particle missing particle name");
					this.MaterialFlag = MaterialFlags.Defaulted;
				}
				else
				{
					_deformDecl = idE.DeclManager.FindType(DeclType.Table, token.ToString(), true);

					_deformRegisters[0] = ParseExpression(lexer);
					_deformRegisters[1] = ParseExpression(lexer);
					_deformRegisters[2] = ParseExpression(lexer);
				}
			}
			else if(tokenLower == "eyeball")
			{
				_deformType = DeformType.Eyeball;
			}
			else if(tokenLower == "particle")
			{
				_deformType = DeformType.Particle;

				if((token = lexer.ExpectAnyToken()) == null)
				{
					lexer.Warning("deform particle missing particle name");
					this.MaterialFlag = MaterialFlags.Defaulted;
				}
				else
				{
					_deformDecl = idE.DeclManager.FindType(DeclType.Particle, token.ToString(), true);
				}
			}
			else if(tokenLower == "particle2")
			{
				_deformType = DeformType.Particle2;

				if((token = lexer.ExpectAnyToken()) == null)
				{
					lexer.Warning("deform particle missing particle name");
					this.MaterialFlag = MaterialFlags.Defaulted;
				}
				else
				{
					_deformDecl = idE.DeclManager.FindType(DeclType.Table, token.ToString(), true);

					_deformRegisters[0] = ParseExpression(lexer);
					_deformRegisters[1] = ParseExpression(lexer);
					_deformRegisters[2] = ParseExpression(lexer);
				}
			}
			else
			{
				lexer.Warning("Bad deform type '{0}'", tokenValue);
				this.MaterialFlag = MaterialFlags.Defaulted;
			}
		}

		private void ParseDecalInfo(idLexer lexer)
		{
			_decalInfo.StayTime = (int) lexer.ParseFloat() * 1000;
			_decalInfo.FadeTime = (int) lexer.ParseFloat() * 1000;
			_decalInfo.Start = lexer.Parse1DMatrix(4);
			_decalInfo.End = lexer.Parse1DMatrix(4);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lexer"></param>
		/// <returns>A register index.</returns>
		private int ParseExpression(idLexer lexer)
		{
			return ParseExpressionPriority(lexer, TopPriority);
		}

		private int ParseExpressionPriority(idLexer lexer, int priority)
		{
			idToken token;

			if(priority == 0)
			{
				return ParseTerm(lexer);
			}

			int a = ParseExpressionPriority(lexer, priority - 1);

			if(TestMaterialFlag(MaterialFlags.Defaulted) == true)
			{
				// we have a parse error.
				return 0;
			}

			if((token = lexer.ReadToken()) == null)
			{
				// we won't get EOF in a real file, but we can
				// when parsing from generated strings
				return a;
			}

			string tokenValue = token.ToString();

			if((priority == 1) && (tokenValue == "*"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.Multiply, priority);
			}
			else if((priority == 1) && (tokenValue == "/"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.Divide, priority);
			}
			else if((priority == 1) && (tokenValue == "%"))
			{
				// implied truncate both to integer.
				return ParseEmitOp(lexer, a, ExpressionOperationType.Modulo, priority);
			}
			else if((priority == 2) && (tokenValue == "+"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.Add, priority);
			}
			else if((priority == 2) && (tokenValue == "-"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.Subtract, priority);
			}
			else if((priority == 3) && (tokenValue == ">"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.GreaterThan, priority);
			}
			else if((priority == 3) && (tokenValue == ">="))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.GreaterThanOrEquals, priority);
			}
			else if((priority == 3) && (tokenValue == ">"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.GreaterThan, priority);
			}
			else if((priority == 3) && (tokenValue == ">="))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.GreaterThanOrEquals, priority);
			}
			else if((priority == 3) && (tokenValue == "<"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.LessThan, priority);
			}
			else if((priority == 3) && (tokenValue == "<="))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.LessThanOrEquals, priority);
			}
			else if((priority == 3) && (tokenValue == "=="))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.Equals, priority);
			}
			else if((priority == 3) && (tokenValue == "!="))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.NotEquals, priority);
			}
			else if((priority == 4) && (tokenValue == "&&"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.And, priority);
			}
			else if((priority == 4) && (tokenValue == "||"))
			{
				return ParseEmitOp(lexer, a, ExpressionOperationType.Or, priority);
			}

			// assume that anything else terminates the expression
			// not too robust error checking...

			lexer.UnreadToken = token;

			return a;
		}

		private int ParseEmitOp(idLexer lexer, int a, ExpressionOperationType opType, int priority)
		{
			int b = ParseExpressionPriority(lexer, priority);

			return EmitOp(a, b, opType);
		}

		private int EmitOp(int a, int b, ExpressionOperationType opType)
		{
			// optimize away identity operations.
			if(opType == ExpressionOperationType.Add)
			{
				if((_parsingData.RegisterIsTemporary[a] == false) && (_parsingData.ShaderRegisters[a] == 0))
				{
					return b;
				}
				else if((_parsingData.RegisterIsTemporary[b] == false) && (_parsingData.ShaderRegisters[b] == 0))
				{
					return a;
				}
				else if((_parsingData.RegisterIsTemporary[a] == false) && (_parsingData.RegisterIsTemporary[b] == false))
				{
					return GetExpressionConstant(_parsingData.ShaderRegisters[a] + _parsingData.ShaderRegisters[b]);
				}
			}
			else if(opType == ExpressionOperationType.Multiply)
			{
				if((_parsingData.RegisterIsTemporary[a] == false) && (_parsingData.ShaderRegisters[a] == 1))
				{
					return b;
				}
				else if((_parsingData.RegisterIsTemporary[a] == false) && (_parsingData.ShaderRegisters[a] == 0))
				{
					return a;
				}
				else if((_parsingData.RegisterIsTemporary[b] == false) && (_parsingData.ShaderRegisters[b] == 1))
				{
					return a;
				}
				else if((_parsingData.RegisterIsTemporary[b] == false) && (_parsingData.ShaderRegisters[b] == 0))
				{
					return b;
				}
				else if((_parsingData.RegisterIsTemporary[a] == false) && (_parsingData.RegisterIsTemporary[b] == false))
				{
					return GetExpressionConstant(_parsingData.ShaderRegisters[a] * _parsingData.ShaderRegisters[b]);
				}
			}

			ExpressionOperation op = GetExpressionOperation();
			op.OperationType = opType;
			op.A = a;
			op.B = b;
			op.C = GetExpressionTemporary();

			return op.C;
		}

		/// <summary>
		/// See if the current token matches one of the surface parameter bit flags.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		private bool CheckSurfaceParameter(idToken token)
		{
			string tokenLower = token.ToString().ToLower();

			foreach(MaterialInfoParameter infoParameter in InfoParameters)
			{
				if(tokenLower == infoParameter.Name)
				{
					if((infoParameter.SurfaceFlags & Renderer.SurfaceFlags.TypeMask) == Renderer.SurfaceFlags.TypeMask)
					{
						// ensure we only have one surface type set
						_surfaceFlags &= ~SurfaceFlags.TypeMask;
					}

					_surfaceFlags |= infoParameter.SurfaceFlags;
					_contentFlags |= infoParameter.ContentFlags;

					if(infoParameter.ClearSolid == true)
					{
						_contentFlags &= ~ContentFlags.Solid;
					}

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lexer"></param>
		/// <returns>A register index.</returns>
		private int ParseTerm(idLexer lexer)
		{
			idToken token = lexer.ReadToken();

			string tokenValue = token.ToString();
			string tokenLower = tokenValue.ToLower();

			if(tokenValue == "(")
			{
				int a = ParseExpression(lexer);
				MatchToken(lexer, ")");

				return a;
			}

			ExpressionRegister tmpReg;

			if(Enum.TryParse<ExpressionRegister>(tokenValue, true, out tmpReg) == true)
			{
				_parsingData.RegistersAreConstant = false;
				return (int) tmpReg;
			}
			
			if(tokenLower == "fragmentPrograms")
			{
				return GetExpressionConstant(1.0f);
			}
			else if(tokenLower == "sound")
			{
				_parsingData.RegistersAreConstant = false;

				return EmitOp(0, 0, ExpressionOperationType.Sound);
			}
			// parse negative numbers
			else if(tokenLower == "-")
			{
				token = lexer.ReadToken();

				if((token.Type == TokenType.Number) || (token.ToString() == "."))
				{
					return GetExpressionConstant(-token.ToFloat());
				}

				lexer.Warning("Bad negative number '{0}'", token.ToString());
				this.MaterialFlag = MaterialFlags.Defaulted;

				return 0;
			}
			else if((token.Type == TokenType.Number) || (tokenValue == ".") || (tokenValue == "-"))
			{
				return GetExpressionConstant(token.ToFloat());
			}

			// see if it is a table name
			idDeclTable table = idE.DeclManager.FindType<idDeclTable>(DeclType.Table, tokenValue, false);

			if(table == null)
			{
				lexer.Warning("Bad term '{0}'", tokenValue);
				this.MaterialFlag = MaterialFlags.Defaulted;

				return 0;
			}

			// parse a table expression
			MatchToken(lexer, "[");

			int b = ParseExpression(lexer);

			MatchToken(lexer, "]");

			return EmitOp(table.Index, b, ExpressionOperationType.Table);
		}

		private void ParseStage(idLexer lexer, TextureRepeat textureRepeatDefault)
		{
			TextureFilter textureFilter = TextureFilter.Default;
			TextureRepeat textureRepeat = textureRepeatDefault;
			TextureDepth textureDepth = TextureDepth.Default;
			CubeFiles cubeMap = CubeFiles.TwoD;

			bool allowPicmip = true;
			string imageName = string.Empty;

			NewMaterialStage newStage = new NewMaterialStage();
			newStage.VertexParameters = new int[4, 4];

			MaterialStage materialStage = new MaterialStage();
			materialStage.ConditionRegister = GetExpressionConstant(1);
			materialStage.Color.Registers = new int[] {
				GetExpressionConstant(1),
				GetExpressionConstant(1),
				GetExpressionConstant(1),
				GetExpressionConstant(1)
			};

			int[,] matrix = new int[2, 3];

			idToken token;
			int a, b;

			string tokenValue;
			string tokenLower;

			while(true)
			{
				if(TestMaterialFlag(MaterialFlags.Defaulted) == true)
				{
					// we have a parse error
					return;
				}
				else if((token = lexer.ExpectAnyToken()) == null)
				{
					this.MaterialFlag = MaterialFlags.Defaulted;
					return;
				}

				tokenValue = token.ToString();
				tokenLower = tokenValue.ToLower();

				// the close brace for the entire material ends the draw block
				if(tokenLower == "}")
				{
					break;
				}
				// BSM Nerve: Added for stage naming in the material editor
				else if(tokenLower == "name")
				{
					lexer.SkipRestOfLine();
				}
				// image options
				else if(tokenLower == "blend")
				{
					ParseBlend(lexer, ref materialStage);
				}
				else if(tokenLower == "map")
				{
					imageName = ParsePastImageProgram(lexer);
				}
				else if(tokenLower == "remoterendermap")
				{
					materialStage.Texture.Dynamic = DynamicImageType.RemoteRender;
					materialStage.Texture.Width = lexer.ParseInt();
					materialStage.Texture.Height = lexer.ParseInt();
				}
				else if(tokenLower == "mirrorrendermap")
				{
					materialStage.Texture.Dynamic = DynamicImageType.MirrorRender;
					materialStage.Texture.Width = lexer.ParseInt();
					materialStage.Texture.Height = lexer.ParseInt();
					materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.Screen;
				}
				else if(tokenLower == "xrayrendermap")
				{
					materialStage.Texture.Dynamic = DynamicImageType.XRayRender;
					materialStage.Texture.Width = lexer.ParseInt();
					materialStage.Texture.Height = lexer.ParseInt();
					materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.Screen;
				}
				else if(tokenLower == "screen")
				{
					materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.Screen;
				}
				else if(tokenLower == "screen2")
				{
					materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.Screen;
				}
				else if(tokenLower == "glasswarp")
				{
					materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.GlassWarp;
				}
				else if(tokenLower == "videomap")
				{
					// note that videomaps will always be in clamp mode, so texture
					// coordinates had better be in the 0 to 1 range
					if((token = lexer.ReadToken()) == null)
					{
						idConsole.Warning("missing parameter for 'videoMap' keyword in material '{0}'", this.Name);
					}
					else
					{
						bool loop = false;

						if(token.ToString().Equals("loop", StringComparison.OrdinalIgnoreCase) == true)
						{
							loop = true;

							if((token = lexer.ReadToken()) == null)
							{
								idConsole.Warning("missing parameter for 'videoMap' keyword in material '{0}'", this.Name);
								continue;
							}
						}

						idConsole.Warning("TODO: material videoMap keyword");

						// TODO: cinematic
						/*ts->cinematic = idCinematic::Alloc();
						ts->cinematic->InitFromFile( token.c_str(), loop );*/
					}
				}
				else if(tokenLower == "soundmap")
				{
					if((token = lexer.ReadToken()) == null)
					{
						idConsole.Warning("missing parameter for 'soundMap' keyword in material '{0}'", this.Name);
					}
					else
					{
						idConsole.Warning("TODO: material soundMap keyword");

						// TODO
						/*ts->cinematic = new idSndWindow();
						ts->cinematic->InitFromFile( token.c_str(), true );*/
					}
				}
				else if(tokenLower == "cubemap")
				{
					imageName = ParsePastImageProgram(lexer);
					cubeMap = CubeFiles.Native;
				}
				else if(tokenLower == "cameracubemap")
				{
					imageName = ParsePastImageProgram(lexer);
					cubeMap = CubeFiles.Camera;
				}
				else if(tokenLower == "ignorealphatest")
				{
					materialStage.IgnoreAlphaTest = true;
				}
				else if(tokenLower == "nearest")
				{
					textureFilter = TextureFilter.Nearest;
				}
				else if(tokenLower == "linear")
				{
					textureFilter = TextureFilter.Linear;
				}
				else if(tokenLower == "clamp")
				{
					textureRepeat = TextureRepeat.Clamp;
				}
				else if(tokenLower == "noclamp")
				{
					textureRepeat = TextureRepeat.Repeat;
				}
				else if(tokenLower == "zeroclamp")
				{
					textureRepeat = TextureRepeat.ClampToZero;
				}
				else if(tokenLower == "alphazeroclamp")
				{
					textureRepeat = TextureRepeat.ClampToZeroAlpha;
				}
				else if((tokenLower == "uncompressed")
					|| (tokenLower == "highquality"))
				{
					if(idE.CvarSystem.GetInteger("image_ignoreHighQuality") == 0)
					{
						textureDepth = TextureDepth.HighQuality;
					}
				}
				else if(tokenLower == "forcehighquality")
				{
					textureDepth = TextureDepth.HighQuality;
				}
				else if(tokenLower == "nopicmip")
				{
					allowPicmip = false;
				}
				else if(tokenLower == "vertexcolor")
				{
					materialStage.VertexColor = StageVertexColor.Modulate;
				}
				else if(tokenLower == "inversevertexcolor")
				{
					materialStage.VertexColor = StageVertexColor.InverseModulate;
				}
				// privatePolygonOffset
				else if(tokenLower == "privatepolygonoffset")
				{
					if((token = lexer.ReadTokenOnLine()) == null)
					{
						materialStage.PrivatePolygonOffset = 1;
					}
					else
					{
						// explict larger (or negative) offset
						lexer.UnreadToken = token;
						materialStage.PrivatePolygonOffset = lexer.ParseFloat();
					}
				}
				// texture coordinate generation
				else if(tokenLower == "texgen")
				{
					token = lexer.ExpectAnyToken();
					tokenValue = token.ToString();
					tokenLower = tokenValue.ToLower();

					if(tokenLower == "normal")
					{
						materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.DiffuseCube;
					}
					else if(tokenLower == "reflect")
					{
						materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.ReflectCube;
					}
					else if(tokenLower == "skybox")
					{
						materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.SkyboxCube;
					}
					else if(tokenLower == "wobblesky")
					{
						materialStage.Texture.TextureCoordinates = TextureCoordinateGeneration.WobbleSkyCube;

						_texGenRegisters = new int[4];
						_texGenRegisters[0] = ParseExpression(lexer);
						_texGenRegisters[1] = ParseExpression(lexer);
						_texGenRegisters[2] = ParseExpression(lexer);
					}
					else
					{
						idConsole.Warning("bad texGen '{0}' in material {1}", tokenValue, this.Name);
						this.MaterialFlag = MaterialFlags.Defaulted;
					}
				}
				else if((tokenLower == "scroll")
					|| (tokenLower == "translate"))
				{
					a = ParseExpression(lexer);
					MatchToken(lexer, ",");
					b = ParseExpression(lexer);

					matrix[0, 0] = GetExpressionConstant(1);
					matrix[0, 1] = GetExpressionConstant(0);
					matrix[0, 2] = a;

					matrix[1, 0] = GetExpressionConstant(0);
					matrix[1, 1] = GetExpressionConstant(1);
					matrix[1, 2] = b;

					MultiplyTextureMatrix(ref materialStage.Texture, matrix);
				}
				else if(tokenLower == "scale")
				{
					a = ParseExpression(lexer);
					MatchToken(lexer, ",");
					b = ParseExpression(lexer);

					// this just scales without a centering
					matrix[0, 0] = a;
					matrix[0, 1] = GetExpressionConstant(0);
					matrix[0, 2] = GetExpressionConstant(0);

					matrix[1, 0] = GetExpressionConstant(0);
					matrix[1, 1] = b;
					matrix[1, 2] = GetExpressionConstant(0);

					MultiplyTextureMatrix(ref materialStage.Texture, matrix);
				}
				else if(tokenLower == "centerscale")
				{
					a = ParseExpression(lexer);
					MatchToken(lexer, ",");
					b = ParseExpression(lexer);

					// this subtracts 0.5, then scales, then adds 0.5
					matrix[0, 0] = a;
					matrix[0, 1] = GetExpressionConstant(0);
					matrix[0, 2] = EmitOp(GetExpressionConstant(0.5f), EmitOp(GetExpressionConstant(0.5f), a, ExpressionOperationType.Multiply), ExpressionOperationType.Subtract);

					matrix[1, 0] = GetExpressionConstant(0);
					matrix[1, 1] = b;
					matrix[1, 2] = EmitOp(GetExpressionConstant(0.5f), EmitOp(GetExpressionConstant(0.5f), b, ExpressionOperationType.Multiply), ExpressionOperationType.Subtract);

					MultiplyTextureMatrix(ref materialStage.Texture, matrix);
				}
				else if(tokenLower == "shear")
				{
					a = ParseExpression(lexer);
					MatchToken(lexer, ",");
					b = ParseExpression(lexer);

					// this subtracts 0.5, then shears, then adds 0.5
					matrix[0, 0] = GetExpressionConstant(1);
					matrix[0, 1] = a;
					matrix[0, 2] = EmitOp(GetExpressionConstant(-0.5f), a, ExpressionOperationType.Multiply);

					matrix[1, 0] = b;
					matrix[1, 1] = GetExpressionConstant(1);
					matrix[1, 2] = EmitOp(GetExpressionConstant(-0.5f), b, ExpressionOperationType.Multiply);

					MultiplyTextureMatrix(ref materialStage.Texture, matrix);
				}
				else if(tokenLower == "rotate")
				{
					int sinReg, cosReg;

					// in cycles
					a = ParseExpression(lexer);

					idDeclTable table = idE.DeclManager.FindType<idDeclTable>(DeclType.Table, "sinTable", false);

					if(table == null)
					{
						idConsole.Warning("no sinTable for rotate defined");
						this.MaterialFlag = MaterialFlags.Defaulted;

						return;
					}

					sinReg = EmitOp(table.Index, a, ExpressionOperationType.Table);
					table = idE.DeclManager.FindType<idDeclTable>(DeclType.Table, "cosTable", false);

					if(table == null)
					{
						idConsole.Warning("no cosTable for rotate defined");
						this.MaterialFlag = MaterialFlags.Defaulted;

						return;
					}

					cosReg = EmitOp(table.Index, a, ExpressionOperationType.Table);

					// this subtracts 0.5, then rotates, then adds 0.5
					matrix[0, 0] = cosReg;
					matrix[0, 1] = EmitOp(GetExpressionConstant(0), sinReg, ExpressionOperationType.Subtract);
					matrix[0, 2] = EmitOp(EmitOp(EmitOp(GetExpressionConstant(-0.5f), cosReg, ExpressionOperationType.Multiply),
									EmitOp(GetExpressionConstant(0.5f), sinReg, ExpressionOperationType.Multiply), ExpressionOperationType.Add),
										GetExpressionConstant(0.5f), ExpressionOperationType.Add);

					matrix[1, 0] = sinReg;
					matrix[1, 1] = cosReg;
					matrix[1, 2] = EmitOp(EmitOp(EmitOp(GetExpressionConstant(-0.5f), sinReg, ExpressionOperationType.Multiply),
									EmitOp(GetExpressionConstant(-0.5f), cosReg, ExpressionOperationType.Multiply), ExpressionOperationType.Add),
										GetExpressionConstant(0.5f), ExpressionOperationType.Add);

					MultiplyTextureMatrix(ref materialStage.Texture, matrix);
				}
				// color mask options
				else if(tokenLower == "maskred")
				{
					materialStage.DrawStateBits |= MaterialStates.RedMask;
				}
				else if(tokenLower == "maskgreen")
				{
					materialStage.DrawStateBits |= MaterialStates.GreenMask;
				}
				else if(tokenLower == "maskblue")
				{
					materialStage.DrawStateBits |= MaterialStates.BlueMask;
				}
				else if(tokenLower == "maskalpha")
				{
					materialStage.DrawStateBits |= MaterialStates.AlphaMask;
				}
				else if(tokenLower == "maskcolor")
				{
					materialStage.DrawStateBits |= MaterialStates.ColorMask;
				}
				else if(tokenLower == "maskdepth")
				{
					materialStage.DrawStateBits |= MaterialStates.DepthMask;
				}
				else if(tokenLower == "alphatest")
				{
					materialStage.HasAlphaTest = true;
					materialStage.AlphaTestRegister = ParseExpression(lexer);

					_coverage = MaterialCoverage.Perforated;
				}
				// shorthand for 2D modulated
				else if(tokenLower == "colored")
				{
					materialStage.Color.Registers[0] = (int) ExpressionRegister.Parm0;
					materialStage.Color.Registers[1] = (int) ExpressionRegister.Parm1;
					materialStage.Color.Registers[2] = (int) ExpressionRegister.Parm2;
					materialStage.Color.Registers[3] = (int) ExpressionRegister.Parm3;

					_parsingData.RegistersAreConstant = false;
				}
				else if(tokenLower == "color")
				{
					materialStage.Color.Registers[0] = ParseExpression(lexer);
					MatchToken(lexer, ",");

					materialStage.Color.Registers[1] = ParseExpression(lexer);
					MatchToken(lexer, ",");

					materialStage.Color.Registers[2] = ParseExpression(lexer);
					MatchToken(lexer, ",");

					materialStage.Color.Registers[3] = ParseExpression(lexer);
				}
				else if(tokenLower == "red")
				{
					materialStage.Color.Registers[0] = ParseExpression(lexer);
				}
				else if(tokenLower == "green")
				{
					materialStage.Color.Registers[1] = ParseExpression(lexer);
				}
				else if(tokenLower == "blue")
				{
					materialStage.Color.Registers[2] = ParseExpression(lexer);
				}
				else if(tokenLower == "alpha")
				{
					materialStage.Color.Registers[3] = ParseExpression(lexer);
				}
				else if(tokenLower == "rgb")
				{
					materialStage.Color.Registers[0] = materialStage.Color.Registers[1] = materialStage.Color.Registers[2] = ParseExpression(lexer);
				}
				else if(tokenLower == "rgba")
				{
					materialStage.Color.Registers[0] = materialStage.Color.Registers[1] = materialStage.Color.Registers[2] = materialStage.Color.Registers[3] = ParseExpression(lexer);
				}
				else if(tokenLower == "if")
				{
					materialStage.ConditionRegister = ParseExpression(lexer);
				}
				else if(tokenLower == "program")
				{
					if((token = lexer.ReadTokenOnLine()) != null)
					{
						idConsole.Warning("TODO: material program keyword");
						// TODO
						/*newStage.vertexProgram = R_FindARBProgram( GL_VERTEX_PROGRAM_ARB, token.c_str() );
						newStage.fragmentProgram = R_FindARBProgram( GL_FRAGMENT_PROGRAM_ARB, token.c_str() );*/
					}
				}
				else if(tokenLower == "fragmentprogram")
				{
					if((token = lexer.ReadTokenOnLine()) != null)
					{
						idConsole.Warning("TODO: material fragmentProgram keyword");
						// TODO
						//newStage.fragmentProgram = R_FindARBProgram( GL_FRAGMENT_PROGRAM_ARB, token.c_str() );
					}
				}
				else if(tokenLower == "vertexprogram")
				{
					if((token = lexer.ReadTokenOnLine()) != null)
					{
						idConsole.Warning("TODO: material vertexProgram keyword");
						// TODO
						//newStage.vertexProgram = R_FindARBProgram( GL_VERTEX_PROGRAM_ARB, token.c_str() );
					}
				}
				else if(tokenLower == "megatexture")
				{
					if((token = lexer.ReadTokenOnLine()) != null)
					{
						idConsole.Warning("TODO: material megaTexture keyword");
						// TODO
						/*newStage.megaTexture = new idMegaTexture;
						if ( !newStage.megaTexture->InitFromMegaFile( token.c_str() ) ) {
							delete newStage.megaTexture;
							SetMaterialFlag( MF_DEFAULTED );
							continue;
						}
						newStage.vertexProgram = R_FindARBProgram( GL_VERTEX_PROGRAM_ARB, "megaTexture.vfp" );
						newStage.fragmentProgram = R_FindARBProgram( GL_FRAGMENT_PROGRAM_ARB, "megaTexture.vfp" );*/
					}
				}
				else if(tokenLower == "vertexparm")
				{
					ParseVertexParameter(lexer, ref newStage);
				}
				else if(tokenLower == "fragmentmap")
				{
					ParseFragmentMap(lexer, ref newStage);
				}
				else
				{
					idConsole.Warning("unknown token '{0}' in material '{1}'", tokenValue, this.Name);

					this.MaterialFlag = MaterialFlags.Defaulted;

					return;
				}
			}

			// if we are using newStage, allocate a copy of it
			if((newStage.FragmentProgram != 0) || (newStage.VertexProgram != 0))
			{
				materialStage.NewStage = newStage;
			}

			// select a compressed depth based on what the stage is
			if(textureDepth == TextureDepth.Default)
			{
				switch(materialStage.Lighting)
				{
					case StageLighting.Bump:
						textureDepth = TextureDepth.Bump;
						break;

					case StageLighting.Diffuse:
						textureDepth = TextureDepth.Diffuse;
						break;

					case StageLighting.Specular:
						textureDepth = TextureDepth.Specular;
						break;
				}
			}

			// now load the image with all the parms we parsed
			if((imageName != null) && (imageName != string.Empty))
			{
				materialStage.Texture.Image = idE.ImageManager.ImageFromFile(imageName, textureFilter, allowPicmip, textureRepeat, textureDepth, cubeMap);

				if(materialStage.Texture.Image == null)
				{
					materialStage.Texture.Image = idE.ImageManager.DefaultImage;
				}
			}
			else if(/*TODO: !ts->cinematic &&*/ (materialStage.Texture.Dynamic == 0) && (materialStage.NewStage.IsEmpty == true))
			{
				idConsole.Warning("material '{0}' had stage with no image", this.Name);
				materialStage.Texture.Image = idE.ImageManager.DefaultImage;
			}

			// successfully parsed a stage.
			_parsingData.Stages.Add(materialStage);
		}

		private void ParseBlend(idLexer lexer, ref MaterialStage stage)
		{
			idToken token;

			if((token = lexer.ReadToken()) == null)
			{
				return;
			}

			string tokenValue = token.ToString();
			string tokenLower = tokenValue.ToLower();

			// blending combinations
			if(tokenLower == "blend")
			{
				stage.DrawStateBits = MaterialStates.SourceBlendSourceAlpha | MaterialStates.DestinationBlendOneMinusSourceAlpha;
			}
			else if(tokenLower == "add")
			{
				stage.DrawStateBits = MaterialStates.SourceBlendOne | MaterialStates.DestinationBlendOne;
			}
			else if((tokenLower == "filter") || (tokenLower == "modulate"))
			{
				stage.DrawStateBits = MaterialStates.SourceBlendDestinationColor | MaterialStates.DestinationBlendZero;
			}
			else if(tokenLower == "none")
			{
				// none is used when defining an alpha mask that doesn't draw
				stage.DrawStateBits = MaterialStates.SourceBlendZero | MaterialStates.DestinationBlendOne;
			}
			else if(tokenLower == "bumpmap")
			{
				stage.Lighting = StageLighting.Bump;
			}
			else if(tokenLower == "diffusemap")
			{
				stage.Lighting = StageLighting.Diffuse;
			}
			else if(tokenLower == "specularmap")
			{
				stage.Lighting = StageLighting.Specular;
			}
			else
			{
				MaterialStates sourceBlendMode = GetSourceBlendMode(tokenLower);

				MatchToken(lexer, ",");

				if((token = lexer.ReadToken()) == null)
				{
					return;
				}

				tokenLower = token.ToString().ToLower();

				MaterialStates destinationBlendMode = GetDestinationBlendMode(tokenLower);

				stage.DrawStateBits = sourceBlendMode | destinationBlendMode;
			}
		}

		/// <summary>
		/// Parses a vertex parameter.
		/// </summary>
		/// <remarks>
		/// If there is a single value, it will be repeated across all elements.
		/// If there are two values, 3 = 0.0, 4 = 1.0.
		/// if there are three values, 4 = 1.0.
		/// </remarks>
		/// <param name="lexer"></param>
		/// <param name="newStage"></param>
		private void ParseVertexParameter(idLexer lexer, ref NewMaterialStage newStage)
		{
			idToken token = lexer.ReadTokenOnLine();
			int parm = token.ToInt32();
			int tmp = 0;

			string tokenValue = token.ToString();

			if((int.TryParse(tokenValue, out tmp) == false) || (parm < 0))
			{
				idConsole.Warning("bad vertexParm number");
				this.MaterialFlag = MaterialFlags.Defaulted;

				return;
			}

			newStage.VertexParameters[parm, 0] = ParseExpression(lexer);
			token = lexer.ReadTokenOnLine();
			tokenValue = token.ToString();

			if((tokenValue == string.Empty) || (tokenValue != ","))
			{
				newStage.VertexParameters[parm, 1] = newStage.VertexParameters[parm, 2] = newStage.VertexParameters[parm, 3] = newStage.VertexParameters[parm, 0];
			}
			else
			{
				newStage.VertexParameters[parm, 1] = ParseExpression(lexer);
				token = lexer.ReadTokenOnLine();
				tokenValue = token.ToString();

				if((tokenValue == string.Empty) || (tokenValue != ","))
				{
					newStage.VertexParameters[parm, 2] = GetExpressionConstant(0);
					newStage.VertexParameters[parm, 3] = GetExpressionConstant(1);
				}
				else
				{
					newStage.VertexParameters[parm, 2] = ParseExpression(lexer);
					token = lexer.ReadTokenOnLine();
					tokenValue = token.ToString();

					if((tokenValue == string.Empty) || (tokenValue != ","))
					{
						newStage.VertexParameters[parm, 3] = GetExpressionConstant(1);
					}
					else
					{
						newStage.VertexParameters[parm, 3] = ParseExpression(lexer);
					}
				}
			}
		}

		private void ParseFragmentMap(idLexer lexer, ref NewMaterialStage newStage)
		{
			TextureFilter textureFilter = TextureFilter.Default;
			TextureRepeat textureRepeat = TextureRepeat.Repeat;
			TextureDepth textureDepth = TextureDepth.Default;
			CubeFiles cubeMap = CubeFiles.TwoD;

			bool allowPicmip = true;

			idToken token = lexer.ReadTokenOnLine();
			int unit = token.ToInt32();
			int tmp;

			if((int.TryParse(token.ToString(), out tmp) == false) || (unit < 0))
			{
				idConsole.Warning("bad fragmentMap number");
				this.MaterialFlag = MaterialFlags.Defaulted;

				return;
			}

			// unit 1 is the normal map.. make sure it gets flagged as the proper depth
			if(unit == 1)
			{
				textureDepth = TextureDepth.Bump;
			}

			string tokenValue;
			string tokenLower;

			while(true)
			{
				token = lexer.ReadTokenOnLine();
				tokenValue = token.ToString();
				tokenLower = tokenValue.ToLower();

				if(tokenLower == "cubemap")
				{
					cubeMap = CubeFiles.Native;
				}
				else if(tokenLower == "cameracubemap")
				{
					cubeMap = CubeFiles.Camera;
				}
				else if(tokenLower == "nearest")
				{
					textureFilter = TextureFilter.Nearest;
				}
				else if(tokenLower == "linear")
				{
					textureFilter = TextureFilter.Linear;
				}
				else if(tokenLower == "clamp")
				{
					textureRepeat = TextureRepeat.Clamp;
				}
				else if(tokenLower == "noclamp")
				{
					textureRepeat = TextureRepeat.Repeat;
				}
				else if(tokenLower == "zeroclamp")
				{
					textureRepeat = TextureRepeat.ClampToZero;
				}
				else if(tokenLower == "alphazeroclamp")
				{
					textureRepeat = TextureRepeat.ClampToZeroAlpha;
				}
				else if(tokenLower == "forcehighquality")
				{
					textureDepth = TextureDepth.HighQuality;
				}
				else if((tokenLower == "uncompressed")
					|| (tokenLower == "highquality"))
				{
					if(idE.CvarSystem.GetInteger("image_ignoreHighQuality") == 0)
					{
						textureDepth = TextureDepth.HighQuality;
					}
				}
				else if(tokenLower == "nopicmip")
				{
					allowPicmip = false;
				}
				else
				{
					// assume anything else is the image name.
					lexer.UnreadToken = token;
					break;
				}
			}

			// TODO
			string image = ParsePastImageProgram(lexer);

			// TODO: fragment program images.  remember we use a global
			// sampler state.  it'll ignore these texturemin/max filters.
			idConsole.Warning("TODO: fragment program images");
			/*
			newStage->fragmentProgramImages[unit] = 
				globalImages->ImageFromFile( str, tf, allowPicmip, trp, td, cubeMap );
			if ( !newStage->fragmentProgramImages[unit] ) {
				newStage->fragmentProgramImages[unit] = globalImages->defaultImage;
			}*/
		}

		private string ParsePastImageProgram(idLexer lexer)
		{
			idImageProgramParser parser = new idImageProgramParser();
			parser.ParseImageProgram(lexer);

			return parser.Source;
		}

		private void AppendToken(StringBuilder b, idToken token)
		{
			if(b.Length > 0)
			{
				b.Append(' ');
			}

			b.Append(token.ToFloat());
		}

		private void MatchAndAppendToken(StringBuilder b, idLexer lexer, string match)
		{
			if(lexer.ExpectTokenString(match) == false)
			{
				return;
			}

			// a matched token won't need a leading space.
			b.Append(match);
		}

		/// <summary>
		/// Sets defaultShader and returns false if the next token doesn't match.
		/// </summary>
		/// <param name="lexer"></param>
		/// <param name="match"></param>
		/// <returns></returns>
		private bool MatchToken(idLexer lexer, string match)
		{
			if(lexer.ExpectTokenString(match) == false)
			{
				this.MaterialFlag = MaterialFlags.Defaulted;

				return false;
			}

			return true;
		}

		private int GetExpressionConstant(float f)
		{
			int i = 0;

			for(i = PredefinedRegisterCount; i < _registerCount; i++)
			{
				if((_parsingData.RegisterIsTemporary[i] == false) && (_parsingData.ShaderRegisters[i] == f))
				{
					return i;
				}
			}

			if(_registerCount == idE.MaxExpressionRegisters)
			{
				idConsole.Warning("GetExpressionConstant: material '{0}' hit MAX_EXPRESSION_REGISTERS", this.Name);
				this.MaterialFlag = MaterialFlags.Defaulted;

				return 0;
			}

			_parsingData.RegisterIsTemporary[i] = false;
			_parsingData.ShaderRegisters[i] = f;
			_registerCount++;

			return i;
		}

		private ExpressionOperation GetExpressionOperation()
		{
			ExpressionOperation op = new ExpressionOperation();
			_parsingData.Operations.Add(op);

			return op;
		}

		private int GetExpressionTemporary()
		{
			if(_registerCount == idE.MaxExpressionRegisters)
			{
				idConsole.Warning("GetExpressionConstant: material '{0}' hit MAX_EXPRESSION_REGISTERS", this.Name);
				this.MaterialFlag = MaterialFlags.Defaulted;

				return 0;
			}

			_parsingData.RegisterIsTemporary[_registerCount] = true;
			_registerCount++;

			return (_registerCount - 1);
		}

		private void MultiplyTextureMatrix(ref TextureStage textureStage, int[,] registers)
		{
			if(textureStage.HasMatrix == false)
			{
				textureStage.HasMatrix = true;
				textureStage.Matrix = registers;
				return;
			}

			int[,] old = textureStage.Matrix;

			// multiply the two matricies.
			textureStage.Matrix[0, 0] = EmitOp(
											EmitOp(old[0, 0], registers[0, 0], ExpressionOperationType.Multiply),
											EmitOp(old[0, 1], registers[1, 0], ExpressionOperationType.Multiply), ExpressionOperationType.Add);

			textureStage.Matrix[0, 1] = EmitOp(
											EmitOp(old[0, 0], registers[0, 1], ExpressionOperationType.Multiply),
											EmitOp(old[0, 1], registers[1, 1], ExpressionOperationType.Multiply), ExpressionOperationType.Add);

			textureStage.Matrix[0, 2] = EmitOp(
											EmitOp(
												EmitOp(old[0, 0], registers[0, 2], ExpressionOperationType.Multiply),
												EmitOp(old[0, 1], registers[1, 2], ExpressionOperationType.Multiply), ExpressionOperationType.Add),
													old[0, 2], ExpressionOperationType.Add);

			textureStage.Matrix[1, 0] = EmitOp(
											EmitOp(old[1, 0], registers[0, 0], ExpressionOperationType.Multiply),
											EmitOp(old[1, 1], registers[1, 0], ExpressionOperationType.Multiply), ExpressionOperationType.Add);

			textureStage.Matrix[1, 1] = EmitOp(
											EmitOp(old[1, 0], registers[0, 1], ExpressionOperationType.Multiply),
											EmitOp(old[1, 1], registers[1, 1], ExpressionOperationType.Multiply), ExpressionOperationType.Add);

			textureStage.Matrix[1, 2] = EmitOp(
											EmitOp(
												EmitOp(old[1, 0], registers[0, 1], ExpressionOperationType.Multiply),
												EmitOp(old[1, 1], registers[1, 1], ExpressionOperationType.Multiply), ExpressionOperationType.Add),
													old[1, 2], ExpressionOperationType.Add);
		}

		private MaterialStates GetSourceBlendMode(string name)
		{
			name = name.ToUpper();

			if(name == "GL_ONE")
			{
				return MaterialStates.SourceBlendOne;
			}
			else if(name == "GL_ZERO")
			{
				return MaterialStates.SourceBlendZero;
			}
			else if(name == "GL_DST_COLOR")
			{
				return MaterialStates.SourceBlendDestinationColor;
			}
			else if(name == "GL_ONE_MINUS_DST_COLOR")
			{
				return MaterialStates.SourceBlendOneMinusDestinationColor;
			}
			else if(name == "GL_SRC_ALPHA")
			{
				return MaterialStates.SourceBlendSourceAlpha;
			}
			else if(name == "GL_ONE_MINUS_SRC_ALPHA")
			{
				return MaterialStates.SourceBlendOneMinusSourceAlpha;
			}
			else if(name == "GL_DST_ALPHA")
			{
				return MaterialStates.SourceBlendDestinationAlpha;
			}
			else if(name == "GL_ONE_MINUS_DST_ALPHA")
			{
				return MaterialStates.SourceBlendOneMinusDestinationAlpha;
			}
			else if(name == "GL_SRC_ALPHA_SATURATE")
			{
				return MaterialStates.SourceBlendAlphaSaturate;
			}

			idConsole.Warning("unknown blend mode '{0}' in material '{1}'", name, this.Name);
			this.MaterialFlag = MaterialFlags.Defaulted;

			return MaterialStates.SourceBlendOne;
		}

		private MaterialStates GetDestinationBlendMode(string name)
		{
			name = name.ToUpper();

			if(name == "GL_ONE")
			{
				return MaterialStates.DestinationBlendOne;
			}
			else if(name == "GL_ZERO")
			{
				return MaterialStates.DestinationBlendZero;
			}
			else if(name == "GL_SRC_ALPHA")
			{
				return MaterialStates.DestinationBlendSourceAlpha;
			}
			else if(name == "GL_ONE_MINUS_SRC_ALPHA")
			{
				return MaterialStates.DestinationBlendOneMinusSourceAlpha;
			}
			else if(name == "GL_DST_ALPHA")
			{
				return MaterialStates.DestinationBlendDestinationAlpha;
			}
			else if(name == "GL_ONE_MINUS_DST_ALPHA")
			{
				return MaterialStates.DestinationBlendOneMinusDestinationAlpha;
			}
			else if(name == "GL_SRC_COLOR")
			{
				return MaterialStates.DestinationBlendSourceColor;
			}
			else if(name == "GL_ONE_MINUS_SRC_COLOR")
			{
				return MaterialStates.DestinationBlendOneMinusSourceColor;
			}

			idConsole.Warning("unknown blend mode '{0}' in material '{1}'", name, this.Name);
			this.MaterialFlag = MaterialFlags.Defaulted;

			return MaterialStates.DestinationBlendOne;
		}

		/// <summary>
		/// As of 5/2/03, about half of the unique materials loaded on typical
		/// maps are constant, but 2/3 of the surface references are.
		/// This is probably an optimization of dubious value.
		/// </summary>
		private void CheckForConstantRegisters()
		{
			if(_parsingData.RegistersAreConstant == false)
			{
				return;
			}

			// evaluate the registers once and save them.
			_constantRegisters = new float[_registerCount];
			float[] materialParms = new float[idE.MaxEntityMaterialParameters];

			View viewDef = new View();
			viewDef.RenderView.MaterialParameters = new float[idE.MaxGlobalMaterialParameters];

			EvaluateRegisters(ref _constantRegisters, materialParms, viewDef, null);
		}
		#endregion
		#endregion

		#region idDecl implementation
		public override string GetDefaultDefinition()
		{
			return "{\n\t{\n\t\tblend\tblend\n\t\tmap\t_default\n\t}\n}";
		}

		/// <summary>
		/// Parses the current material definition and finds all necessary images.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public override bool Parse(string text)
		{
			idLexer lexer = new idLexer(idDeclFile.LexerOptions);
			lexer.LoadMemory(text, this.FileName, this.LineNumber);
			lexer.SkipUntilString("{");

			// reset to the unparsed state.
			Clear();

			_parsingData = new MaterialParsingData(); // this is only valid during parsing.

			// parse it
			ParseMaterial(lexer);

			// TODO: fs_copyFiles
			// if we are doing an fs_copyfiles, also reference the editorImage
			/*if ( cvarSystem->GetCVarInteger( "fs_copyFiles" ) ) {
				GetEditorImage();
			}*/

			// count non-lit stages.
			_ambientStageCount = 0;
			_stageCount = _parsingData.Stages.Count;

			for(int i = 0; i < _stageCount; i++)
			{
				if(_parsingData.Stages[i].Lighting == StageLighting.Ambient)
				{
					_ambientStageCount++;
				}
			}

			// see if there is a subview stage
			if(_sort == (float) MaterialSort.Subview)
			{
				_hasSubview = true;
			}
			else
			{
				_hasSubview = false;

				for(int i = 0; i < _parsingData.Stages.Count; i++)
				{
					if(_parsingData.Stages[i].Texture.Dynamic != null)
					{
						_hasSubview = true;
					}
				}
			}

			// automatically determine coverage if not explicitly set.
			if(_coverage == MaterialCoverage.Bad)
			{
				// automatically set MC_TRANSLUCENT if we don't have any interaction stages and 
				// the first stage is blended and not an alpha test mask or a subview.
				if(_stageCount == 0)
				{
					// non-visible.
					_coverage = MaterialCoverage.Translucent;
				}
				else if(_stageCount != _ambientStageCount)
				{
					// we have an interaction draw.
					_coverage = MaterialCoverage.Opaque;
				}
				else
				{
					MaterialStates drawStateBits = _parsingData.Stages[0].DrawStateBits & MaterialStates.SourceBlendBits;

					if(((drawStateBits & MaterialStates.DestinationBlendBits) != MaterialStates.DestinationBlendZero)
						|| (drawStateBits == MaterialStates.SourceBlendDestinationColor)
						|| (drawStateBits == MaterialStates.SourceBlendOneMinusDestinationColor)
						|| (drawStateBits == MaterialStates.SourceBlendDestinationAlpha)
						|| (drawStateBits == MaterialStates.SourceBlendOneMinusDestinationAlpha))
					{
						// blended with the destination
						_coverage = MaterialCoverage.Translucent;
					}
					else
					{
						_coverage = MaterialCoverage.Opaque;
					}
				}
			}

			// translucent automatically implies noshadows.
			if(_coverage == MaterialCoverage.Translucent)
			{
				this.MaterialFlag = MaterialFlags.NoShadows;
			}
			else
			{
				// mark the contents as opaque.
				_contentFlags |= ContentFlags.Opaque;
			}

			// the sorts can make reasonable defaults.
			if(_sort == (float) MaterialSort.Bad)
			{
				if(TestMaterialFlag(MaterialFlags.PolygonOffset) == true)
				{
					_sort = (float) MaterialSort.Decal;
				}
				else if(_coverage == MaterialCoverage.Translucent)
				{
					_sort = (float) MaterialSort.Medium;
				}
				else
				{
					_sort = (float) MaterialSort.Opaque;
				}
			}

			// anything that references _currentRender will automatically get sort = SS_POST_PROCESS
			// and coverage = MC_TRANSLUCENT.
			for(int i = 0; i < _stageCount; i++)
			{
				MaterialStage stage = _parsingData.Stages[i];

				if(stage.Texture.Image == idE.ImageManager.CurrentRenderImage)
				{
					if(_sort != (float) MaterialSort.PortalSky)
					{
						_sort = (float) MaterialSort.PostProcess;
						_coverage = MaterialCoverage.Translucent;
					}

					break;
				}

				if(stage.NewStage.IsEmpty == false)
				{
					NewMaterialStage newShaderStage = stage.NewStage;

					for(int j = 0; j < newShaderStage.FragmentProgramImages.Length; j++)
					{
						if(newShaderStage.FragmentProgramImages[j] == idE.ImageManager.CurrentRenderImage)
						{
							if(_sort != (float) MaterialSort.PortalSky)
							{
								_sort = (float) MaterialSort.PostProcess;
								_coverage = MaterialCoverage.Translucent;
							}

							i = _stageCount;
							break;
						}
					}
				}
			}

			// set the drawStateBits depth flags.
			for(int i = 0; i < _stageCount; i++)
			{
				MaterialStage stage = _parsingData.Stages[i];

				if(_sort == (float) MaterialSort.PostProcess)
				{
					// post-process effects fill the depth buffer as they draw, so only the
					// topmost post-process effect is rendered.
					stage.DrawStateBits |= MaterialStates.DepthFunctionLess;
				}
				else if((_coverage == MaterialCoverage.Translucent) || (stage.IgnoreAlphaTest == true))
				{
					// translucent surfaces can extend past the exactly marked depth buffer.
					stage.DrawStateBits |= MaterialStates.DepthFunctionLess | MaterialStates.DepthMask;
				}
				else
				{
					// opaque and perforated surfaces must exactly match the depth buffer,
					// which gets alpha test correct.
					stage.DrawStateBits |= MaterialStates.DepthFunctionEqual | MaterialStates.DepthMask;
				}

				_parsingData.Stages[i] = stage;
			}

			// determine if this surface will accept overlays / decals.
			if(_parsingData.ForceOverlays == true)
			{
				// explicitly flaged in material definition
				_allowOverlays = true;
			}
			else
			{
				if(this.IsDrawn == false)
				{
					_allowOverlays = false;
				}

				if(this.Coverage != MaterialCoverage.Opaque)
				{
					_allowOverlays = false;
				}

				if((this.SurfaceFlags & Renderer.SurfaceFlags.NoImpact) == Renderer.SurfaceFlags.NoImpact)
				{
					_allowOverlays = false;
				}
			}

			// add a tiny offset to the sort orders, so that different materials
			// that have the same sort value will at least sort consistantly, instead
			// of flickering back and forth.

			/* this messed up in-game guis
			if ( sort != SS_SUBVIEW ) {
				int	hash, l;

				l = name.Length();
				hash = 0;
				for ( int i = 0 ; i < l ; i++ ) {
					hash ^= name[i];
				}
				sort += hash * 0.01;
			}
			*/

			if(_stageCount > 0)
			{
				_stages = _parsingData.Stages.ToArray();
			}

			if(_parsingData.Operations.Count > 0)
			{
				_ops = _parsingData.Operations.ToArray();
			}

			if(_registerCount > 0)
			{
				_expressionRegisters = new float[_registerCount];

				Array.Copy(_parsingData.ShaderRegisters, _expressionRegisters, _registerCount);
			}

			// see if the registers are completely constant, and don't need to be evaluated per-surface.
			CheckForConstantRegisters();

			_parsingData = null;

			// finish things up
			if(TestMaterialFlag(MaterialFlags.Defaulted))
			{
				MakeDefault();
				return false;
			}

			return true;
		}

		protected override bool GenerateDefaultText()
		{
			// if there exists an image with the same name
			if(true)
			{
				this.SourceText = "material " + this.Name + " // IMPLICITLY GENERATED\n"
					+ "{\n{\nblend blend\n"
					+ "colored\n map \"" + this.Name + "\"\nclamp\n}\n}\n";

				return true;
			}

			return false;
		}

		protected override void ClearData()
		{
			_stages = new MaterialStage[] { };
			_expressionRegisters = null;
			_constantRegisters = null;
			_ops = null;
		}
		#endregion
	}

	[Flags]
	public enum MaterialFlags
	{
		Defaulted = 1 << 0,
		PolygonOffset = 1 << 1,
		NoShadows = 1 << 2,
		ForceShadows = 1 << 3,
		NoSelfShadow = 1 << 4,
		/// <summary>This fog volume won't ever consder a portal fogged out.</summary>
		NoPortalFog = 1 << 5,
		/// <summary>In use (visible) per editor.</summary>
		EditorVisible = 1 << 6
	}

	/// <summary>
	/// Content flags.
	/// </summary>
	/// <remarks>
	/// Make sure to keep the defines in doom_defs.script up to date with these!
	/// </remarks>
	[Flags]
	public enum ContentFlags
	{
		/// <summary>An eye is never valid in a solid.</summary>
		Solid = 1 << 0,
		/// <summary>Blocks visibility (for ai).</summary>
		Opaque = 1 << 1,
		/// <summary>Used for water.</summary>
		Water = 1 << 2,
		/// <summary>Solid to players.</summary>
		PlayerClip = 1 << 3,
		/// <summary>Solid to monsters.</summary>
		MonsterClip = 1 << 4,
		/// <summary>Solid to moveable entities.</summary>
		MoveableClip = 1 << 5,
		/// <summary>Solid to IK.</summary>
		IkClip = 1 << 6,
		/// <summary>Used to detect blood decals.</summary>
		Blood = 1 << 7,
		/// <summary>sed for actors.</summary>
		Body = 1 << 8,
		/// <summary>Used for projectiles.</summary>
		Projectile = 1 << 9,
		/// <summary>Used for dead bodies.</summary>
		Corpse = 1 << 10,
		/// <summary>Used for render models for collision detection.</summary>
		RenderModel = 1 << 11,
		/// <summary>Used for triggers.</summary>
		Trigger = 1 << 12,
		/// <summary>Solid for AAS.</summary>
		AasSolid = 1 << 13,
		/// <summary>Used to compile an obstacle into AAS that can be enabled/disabled.</summary>
		AasObstacle = 1 << 14,
		/// <summary>Used for triggers that are activated by the flashlight.</summary>
		FlashlightTrigger = 1 << 15,

		/// <summary>Portal separating renderer areas.</summary>
		AreaPortal = 1 << 20,
		/// <summary>Don't cut this brush with CSG operations in the editor.</summary>
		NoCsg = 1 << 21,

		RemoveUtil = ~(AreaPortal | NoCsg),

		None = 0
	}

	internal sealed class MaterialParsingData
	{
		public bool[] RegisterIsTemporary = new bool[idE.MaxExpressionRegisters];
		public float[] ShaderRegisters = new float[idE.MaxExpressionRegisters];

		public List<ExpressionOperation> Operations = new List<ExpressionOperation>();
		public List<MaterialStage> Stages = new List<MaterialStage>();

		public bool RegistersAreConstant;
		public bool ForceOverlays;
	}

	internal sealed class ExpressionOperation
	{
		public ExpressionOperationType OperationType;
		public int A, B, C;
	}

	internal struct DecalInfo
	{
		/// <summary>
		/// msec for no change.
		/// </summary>
		public int StayTime;

		/// <summary>
		/// msec to fade vertex colors over.
		/// </summary>
		public int FadeTime;

		/// <summary>
		/// Vertex color at spawn (possibly out of 0.0 - 1.0 range, will clamp after calc).
		/// </summary>
		public float[] Start;

		/// <summary>
		/// Vertex color at fade-out (possibly out of 0.0 - 1.0 range, will clamp after calc).
		/// </summary>
		public float[] End;
	}

	public struct MaterialStage
	{
		public int ConditionRegister;				// if registers[conditionRegister] == 0, skip stage.
		public StageLighting Lighting;				// determines which passes interact with lights.

		public MaterialStates DrawStateBits;
		public ColorStage Color;
		public bool HasAlphaTest;
		public int AlphaTestRegister;

		public TextureStage Texture;
		public StageVertexColor VertexColor;

		public bool IgnoreAlphaTest;				// this stage should act as translucent, even if the surface is alpha tested.
		public float PrivatePolygonOffset;			// a per-stage polygon offset.

		public NewMaterialStage NewStage;	// vertex / fragment program based stage.
	}

	public struct TextureStage
	{
		// TODO
		/*idCinematic *		cinematic;*/
		public idImage Image;
		public TextureCoordinateGeneration TextureCoordinates;

		public bool HasMatrix;
		public int[,] Matrix;	// we only allow a subset of the full projection matrix.

		// dynamic image variables
		public DynamicImageType Dynamic;
		public int Width;
		public int Height;
		public int FrameCount;
	}

	public struct ColorStage
	{
		public int[] Registers;
	}

	public struct NewMaterialStage
	{
		#region Properties
		public bool IsEmpty
		{
			get
			{
				return ((VertexProgram == 0) && (VertexParameters == null) && (FragmentProgram == 0));
			}
		}
		#endregion

		#region Fields
		public int VertexProgram;
		public int[,] VertexParameters; // evaluated register indexes.

		public int FragmentProgram;
		public idImage[] FragmentProgramImages;

		// TODO: public idMegaTexture MegaTexture; // handles all the binding and parameter setting.
		#endregion
	}

	public enum SurfaceTypes
	{
		None,
		Metal,
		Stone,
		Flesh,
		Wood,
		Cardboard,
		Liquid,
		Glass,
		Plastic,
		Ricochet,
		Extra1,
		Extra2,
		Extra3,
		Extra4,
		Extra5,
		Extra6
	}

	[Flags]
	public enum MaterialStates
	{
		Invalid = -1,

		DepthMask = 0x00000100,
		RedMask = 0x00000200,
		GreenMask = 0x00000400,
		BlueMask = 0x00000800,
		AlphaMask = 0x00001000,
		ColorMask = RedMask | GreenMask | BlueMask,

		SourceBlendZero = 0x00000001,
		SourceBlendOne = 0x0,
		SourceBlendDestinationColor = 0x00000003,
		SourceBlendOneMinusDestinationColor = 0x00000004,
		SourceBlendSourceAlpha = 0x00000005,
		SourceBlendOneMinusSourceAlpha = 0x00000006,
		SourceBlendDestinationAlpha = 0x00000007,
		SourceBlendOneMinusDestinationAlpha = 0x00000008,
		SourceBlendAlphaSaturate = 0x00000009,
		SourceBlendBits = 0x0000000F,

		DestinationBlendZero = 0x0,
		DestinationBlendOne = 0x00000020,
		DestinationBlendSourceColor = 0x00000030,
		DestinationBlendOneMinusSourceColor = 0x00000040,
		DestinationBlendSourceAlpha = 0x00000050,
		DestinationBlendOneMinusSourceAlpha = 0x00000060,
		DestinationBlendDestinationAlpha = 0x00000070,
		DestinationBlendOneMinusDestinationAlpha = 0x00000080,
		DestinationBlendBits = 0x000000F0,

		PolygonModeLine = 0x00002000,

		DepthFunctionAlways = 0x00010000,
		DepthFunctionEqual = 0x00020000,
		DepthFunctionLess = 0x0,

		AlphaTestEqual255 = 0x10000000,
		AlphaTestLessThan128 = 0x20000000,
		AlphaTestGreaterOrEqual128 = 0x40000000,
		AlphaTestBits = 0x70000000
	}

	[Flags]
	public enum SurfaceFlags
	{
		/// <summary>Encodes the material type (metal, flesh, concrete, etc.).</summary>
		TypeBit0 = 1 << 0,
		TypeBit1 = 1 << 1,
		TypeBit2 = 1 << 2,
		TypeBit3 = 1 << 3,
		TypeMask = (1 << 4) - 1,

		/// <summary>Nver give falling damage.</summary>
		NoDamage = 1 << 4,
		/// <summary>Effects game physics.</summary>
		Slick = 1 << 5,
		/// <summary>Collision surface.</summary>
		Collision = 1 << 6,
		/// <summary>Player can climb up this surface.</summary>
		Ladder = 1 << 7,
		/// <summary>Don't make missile explosions.</summary>
		NoImpact = 1 << 8,
		/// <summary>No footstep sounds.</summary>
		NoSteps = 1 << 9,
		/// <summary>Not clipped or merged by utilities.</summary>
		Discrete = 1 << 10,
		/// <summary>dmap won't cut surface at each bsp boundary.</summary>
		NoFragment = 1 << 11,
		/// <summary>Renderbump will draw this surface as 0x80 0x80 0x80, which won't collect light from any angle</summary>
		NullNormal = 1 << 12,

		None = 0,

		// surface types
		Metal,
		Stone,
		Flesh,
		Wood,
		Cardboard,
		Liquid,
		Glass,
		Plastic,
		Ricochet,
		T10,
		T11,
		T12,
		T13,
		T14,
		T15
	}

	internal enum ExpressionOperationType
	{
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulo,
		Table,
		GreaterThan,
		GreaterThanOrEquals,
		LessThan,
		LessThanOrEquals,
		Equals,
		NotEquals,
		And,
		Or,
		Sound
	}

	public enum ExpressionRegister
	{
		Time = 0,

		Parm0,
		Parm1,
		Parm2,
		Parm3,
		Parm4,
		Parm5,
		Parm6,
		Parm7,
		Parm8,
		Parm9,
		Parm10,
		Parm11,

		Global0,
		Global1,
		Global2,
		Global3,
		Global4,
		Global5,
		Global6,
		Global7
	}

	public enum MaterialCoverage
	{
		Bad,
		/// <summary>Completely fills the triangle, will have black drawn on fillDepthBuffer.</summary>
		Opaque,
		/// <summary>May have alpha tested holes.</summary>
		Perforated,
		/// <summary>Blended with background.</summary>
		Translucent
	}

	public enum MaterialSort
	{
		/// <summary>Mirrors, view screens, etc.</summary>
		Subview = -3,
		Gui = -2,
		Bad = -1,
		Opaque,
		PortalSky,
		/// <summary>Scorch marks, etc.</summary>
		Decal,
		Far,
		/// <summary>Normal translucent.</summary>
		Medium,
		Close,
		/// <summary>Gun smoke puffs.</summary>
		AlmostNearest,
		/// <summary>Screen blood blobs.</summary>
		Nearest,
		/// <summary>After a screen copy to texture.</summary>
		PostProcess = 100
	}

	public enum CullType
	{
		None = -1,
		Front = 1,
		Back,
		TwoSided
	}

	public enum DeformType
	{
		None,
		Sprite,
		Tube,
		Flare,
		Expand,
		Move,
		Eyeball,
		Particle,
		Particle2,
		Turbulent
	}

	public struct MaterialInfoParameter
	{
		public string Name;
		public bool ClearSolid;
		public SurfaceFlags SurfaceFlags;
		public ContentFlags ContentFlags;

		public MaterialInfoParameter(string name, bool clearSolid, SurfaceFlags surfaceFlags, ContentFlags contentFlags)
		{
			Name = name;
			ClearSolid = clearSolid;
			SurfaceFlags = surfaceFlags;
			ContentFlags = contentFlags;
		}
	}
}