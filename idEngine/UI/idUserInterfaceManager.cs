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

using Microsoft.Xna.Framework;

using idTech4.Renderer;
using idTech4.Text;

namespace idTech4.UI
{
	public sealed class idUserInterfaceManager
	{
		#region Properties
		public idDeviceContext Context
		{
			get
			{
				return _deviceContext;
			}
		}

		public idRectangle ScreenRectangle
		{
			get
			{
				return _screenRect;
			}
		}
		#endregion

		#region Members
		private idRectangle _screenRect;
		private idDeviceContext _deviceContext;

		private List<idUserInterface> _guiList = new List<idUserInterface>();
		#endregion

		#region Constructor
		public idUserInterfaceManager()
		{
			new idCvar("gui_debug", "0", "", CvarFlags.Gui | CvarFlags.Integer);
			new idCvar("gui_edit", "0", "", CvarFlags.Gui | CvarFlags.Bool);
			new idCvar("gui_smallFontLimit", "0.30", "", CvarFlags.Gui | CvarFlags.Archive);
			new idCvar("gui_mediumFontLimit", "0.60", "", CvarFlags.Gui | CvarFlags.Archive);
		}
		#endregion

		#region Methods
		#region Public
		public void BeginLevelLoad()
		{
			foreach(idUserInterface userInterface in _guiList)
			{
				if((userInterface.Desktop.Flags & WindowFlags.MenuInterface) == 0)
				{
					userInterface.ClearReferences();
				}
			}
		}

		public void EndLevelLoad()
		{
			int c = _guiList.Count;

			for(int i = 0; i < c; i++)
			{
				if(_guiList[i].ReferenceCount == 0)
				{
					// common->Printf( "purging %s.\n", guis[i]->GetSourceFile() );

					// use this to make sure no materials still reference this gui
					bool remove = true;

					for(int j = 0; j < idE.DeclManager.GetDeclCount(DeclType.Material); j++)
					{
						idMaterial material = (idMaterial) idE.DeclManager.DeclByIndex(DeclType.Material, j, false);

						if(material.GlobalInterface == _guiList[i])
						{
							remove = false;
							break;
						}
					}

					if(remove == true)
					{
						_guiList[i].Dispose();
						_guiList.RemoveAt(i);
						
						i--;
						c--;
					}
				}
			}
		}

		public bool Exists(string path)
		{
			return idE.FileSystem.FileExists(path);
		}
		
		public void Init()
		{
			_screenRect = new idRectangle(0, 0, 640, 480);

			_deviceContext = new idDeviceContext();
			_deviceContext.Init();			
		}

		public idUserInterface FindInterface(string path, bool autoLoad = false, bool needUnique = false, bool forceNotUnique = false)
		{
			foreach(idUserInterface gui in _guiList)
			{
				if(gui.SourceFile.Equals(path, StringComparison.OrdinalIgnoreCase) == true)
				{
					if((forceNotUnique == false) && ((needUnique == true) || (gui.IsInteractive == true)))
					{
						break;
					}

					gui.AddReference();

					return gui;
				}
			}

			if(autoLoad == true)
			{
				idUserInterface gui = new idUserInterface();

				if(gui.InitFromFile(path) == true)
				{
					gui.IsUnique = (forceNotUnique == true) ? false : needUnique;
					return gui;
				}
			}

			return null;
		}

		public void Remove(idUserInterface ui)
		{
			if(_guiList.Contains(ui) == true)
			{
				_guiList.Remove(ui);
			}

			if(ui.Disposed == false)
			{
				ui.Dispose();
			}
		}
		#endregion

		#region Internal
		internal idUserInterface FindInternalInterface(idUserInterface gui)
		{
			return _guiList.Find(u => u == gui);
		}

		internal void AddInternalInterface(idUserInterface gui)
		{
			_guiList.Add(gui);
		}
		#endregion
		#endregion
	}
}