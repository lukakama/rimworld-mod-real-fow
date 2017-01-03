//   Copyright 2017 Luca De Petrillo
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RimWorldRealFoW {
	public static class Detours {

		private static List<string> detoured = new List<string>();
		private static List<string> destinations = new List<string>();

		/**
				This is a basic first implementation of the IL method 'hooks' (detours) made possible by RawCode's work;
				https://ludeon.com/forums/index.php?topic=17143.0
				Performs detours, spits out basic logs and warns if a method is detoured multiple times.
		**/
		public static unsafe bool TryDetourFromTo(MethodInfo source, MethodInfo destination) {
			// error out on null arguments
			if (source == null) {
				Debug.LogError("Detours - Source MethodInfo is null");
				return false;
			}

			if (destination == null) {
				Debug.LogError("Detours - Destination MethodInfo is null");
				return false;
			}

			// keep track of detours and spit out some messaging
			string sourceString = source.DeclaringType.FullName + "." + source.Name + " @ 0x" + source.MethodHandle.GetFunctionPointer().ToString("X" + (IntPtr.Size * 2).ToString());
			string destinationString = destination.DeclaringType.FullName + "." + destination.Name + " @ 0x" + destination.MethodHandle.GetFunctionPointer().ToString("X" + (IntPtr.Size * 2).ToString());

			detoured.Add(sourceString);
			destinations.Add(destinationString);

			if (IntPtr.Size == sizeof(Int64)) {
				// 64-bit systems use 64-bit absolute address and jumps
				// 12 byte destructive

				// Get function pointers
				long Source_Base = source.MethodHandle.GetFunctionPointer().ToInt64();
				long Destination_Base = destination.MethodHandle.GetFunctionPointer().ToInt64();

				// Native source address
				byte* Pointer_Raw_Source = (byte*) Source_Base;

				// Pointer to insert jump address into native code
				long* Pointer_Raw_Address = (long*) (Pointer_Raw_Source + 0x02);

				// Insert 64-bit absolute jump into native code (address in rax)
				// mov rax, immediate64
				// jmp [rax]
				*(Pointer_Raw_Source + 0x00) = 0x48;
				*(Pointer_Raw_Source + 0x01) = 0xB8;
				*Pointer_Raw_Address = Destination_Base; // ( Pointer_Raw_Source + 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 )
				*(Pointer_Raw_Source + 0x0A) = 0xFF;
				*(Pointer_Raw_Source + 0x0B) = 0xE0;

			} else {
				// 32-bit systems use 32-bit relative offset and jump
				// 5 byte destructive

				// Get function pointers
				int Source_Base = source.MethodHandle.GetFunctionPointer().ToInt32();
				int Destination_Base = destination.MethodHandle.GetFunctionPointer().ToInt32();

				// Native source address
				byte* Pointer_Raw_Source = (byte*) Source_Base;

				// Pointer to insert jump address into native code
				int* Pointer_Raw_Address = (int*) (Pointer_Raw_Source + 1);

				// Jump offset (less instruction size)
				int offset = (Destination_Base - Source_Base) - 5;

				// Insert 32-bit relative jump into native code
				*Pointer_Raw_Source = 0xE9;
				*Pointer_Raw_Address = offset;
			}

			// done!
			return true;
		}

	}
}