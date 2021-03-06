﻿/*
===========================================================================

Doom 3 BFG Edition GPL Source Code
Copyright (C) 1993-2012 id Software LLC, a ZeniMax Media company. 

This file is part of the Doom 3 BFG Edition GPL Source Code ("Doom 3 BFG Edition Source Code").  

Doom 3 BFG Edition Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Doom 3 BFG Edition Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Doom 3 BFG Edition Source Code.  If not, see <http://www.gnu.org/licenses/>.

In addition, the Doom 3 BFG Edition Source Code is also subject to certain additional terms. You should have received a copy of these additional terms immediately following the terms and conditions of the GNU General Public License which accompanied the Doom 3 BFG Edition Source Code.  If not, please request a copy in writing from id Software at the address below.

If you have questions concerning this license or the applicable additional terms, you may contact in writing id Software LLC, c/o ZeniMax Media Inc., Suite 120, Rockville, Maryland 20850 USA.

===========================================================================
*/
using System;
using System.Diagnostics;

namespace idTech4.Math
{
	public struct idRectangle
	{
		public float Bottom
		{
			get
			{
				return (this.Y + this.Height);
			}
		}

		public float Right
		{
			get
			{
				return (this.X + this.Width);
			}
		}

		public float X;
		public float Y;
		public float Width;
		public float Height;

		public static idRectangle Empty = new idRectangle();

		public idRectangle(float x, float y, float width, float height)
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		public bool Contains(float x, float y)
		{
			if((this.Width == 0) && (this.Height == 0))
			{
				return false;
			}

			return ((x >= this.X)
				&& (x <= this.Right)
				&& (y >= this.Y)
				&& (y <= this.Bottom));
		}

		public void Offset(float x, float y)
		{
			this.X += x;
			this.Y += y;
		}

		public override int GetHashCode()
		{
			return ((int) X ^ (int) Y ^ (int) Width ^ (int) Height);
		}

		public override bool Equals(object obj)
		{
			if(obj is idRectangle)
			{
				idRectangle r = (idRectangle) obj;

				return ((this.X == r.X) && (this.Y == r.Y) && (this.Width == r.Width) && (this.Height == r.Height));
			}

			return false;
		}

		public static bool operator ==(idRectangle r1, idRectangle r2)
		{
			return ((r1.X == r2.X) && (r1.Y == r2.Y) && (r1.Width == r2.Width) && (r1.Height == r2.Height));
		}

		public static bool operator !=(idRectangle r1, idRectangle r2)
		{
			return ((r1.X != r2.X) || (r1.Y != r2.Y) || (r1.Width != r2.Width) || (r1.Height != r2.Height));
		}

		public static idRectangle Parse(string str)
		{
			try
			{
				string[] parts = null;

				if(str.Contains(",") == true)
				{
					parts = str.Replace(" ", "").Split(',');
				}
				else
				{
					parts = str.Split(' ');
				}

				if(parts.Length == 4)
				{
					return new idRectangle(
						float.Parse(parts[0]),
						float.Parse(parts[1]),
						float.Parse(parts[2]),
						float.Parse(parts[3]));
				}
			}
			catch(Exception x)
			{
				Debug.Write(x.ToString());
			}

			return idRectangle.Empty;
		}
	}
}