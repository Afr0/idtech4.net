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

namespace idTech4
{
	/// <summary>
	/// This is a dictionary class that tracks an arbitrary number of key / value
	/// pair combinations. It is used for map entity spawning, GUI state management,
	/// and other things.
	/// </summary>
	/// <remarks>
	/// Keys are compared case-insensitive.
	/// </remarks>
	public sealed class idDict
	{
		#region Members
		// TODO: is this going to be affected by boxing on primitives?
		private Dictionary<string, object> _dict;
		#endregion 

		#region Constructor
		public idDict()
		{
			_dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region Methods
		#region Public
		public bool GetBool(string key)
		{
			return GetBool(key, false);
		}

		public bool GetBool(string key, bool defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return Convert.ToBoolean(_dict[key]);
			}

			return defaultValue;
		}

		public float GetFloat(string key)
		{
			return GetFloat(key, 0);
		}

		public float GetFloat(string key, float defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return Convert.ToSingle(_dict[key]);
			}

			return defaultValue;
		}

		public float GetInteger(string key)
		{
			return GetInteger(key, 0);
		}

		public float GetInteger(string key, int defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return Convert.ToInt32(_dict[key]);
			}

			return defaultValue;
		}

		public string GetString(string key)
		{
			return GetString(key, string.Empty);
		}

		public string GetString(string key, string defaultString)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return _dict[key].ToString();
			}

			return defaultString;
		}

		public Vector2 GetVector2(string key)
		{
			return GetVector2(key, Vector2.Zero);
		}

		public Vector2 GetVector2(string key, Vector2 defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return idHelper.ParseVector2(_dict[key].ToString());
			}

			return defaultValue;
		}

		public Vector3 GetVector3(string key)
		{
			return GetVector3(key, Vector3.Zero);
		}

		public Vector3 GetVector3(string key, Vector3 defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return idHelper.ParseVector3(_dict[key].ToString());
			}

			return defaultValue;
		}

		public Vector4 GetVector4(string key)
		{
			return GetVector4(key, Vector4.Zero);
		}

		public Vector4 GetVector4(string key, Vector4 defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return idHelper.ParseVector4(_dict[key].ToString());
			}

			return defaultValue;
		}

		public Rectangle GetRectangle(string key)
		{
			return GetRectangle(key, Rectangle.Empty);
		}

		public Rectangle GetRectangle(string key, Rectangle defaultValue)
		{
			if(_dict.ContainsKey(key) == true)
			{
				return idHelper.ParseRectangle(_dict[key].ToString());
			}

			return defaultValue;
		}

		public void Set(string key, string value)
		{
			if((key == null) || (key == string.Empty))
			{
				return;
			}

			if(_dict.ContainsKey(key) == true)
			{
				_dict[key] = value;
			}
			else
			{
				_dict.Add(key, value);
			}
		}

		public void Set(string key, int value)
		{
			Set(key, value.ToString());
		}

		public void Set(string key, float value)
		{
			Set(key, value.ToString());
		}

		public void Set(string key, bool value)
		{
			Set(key, value.ToString());
		}

		public void Set(string key, Vector2 value)
		{
			Set(key, string.Format("{0} {1}", value.X, value.Y));
		}

		public void Set(string key, Vector3 value)
		{
			Set(key, string.Format("{0} {1} {2}", value.X, value.Y, value.Z));
		}

		public void Set(string key, Vector4 value)
		{
			Set(key, string.Format("{0} {1} {2} {3}", value.X, value.Y, value.Z, value.W));
		}

		public void Set(string key, Rectangle value)
		{
			Set(key, string.Format("{0} {1} {2} {3}", value.X, value.Y, value.Width, value.Height));
		}
		#endregion
		#endregion
	}
}