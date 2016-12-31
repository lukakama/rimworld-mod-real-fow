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
using System;
using System.Reflection;

namespace RimWorldRealFoW {

	/**
		 This is a basic first implementation of the IL method 'hooks' (detours) made possible by RawCode's work;
		 https://ludeon.com/forums/index.php?topic=17143.0
		 Performs detours, spits out basic logs and warns if a method is detoured multiple times.
	**/

	public unsafe class DetourHook {
		private byte* Pointer_Raw_Source;

		private byte[] originalBytes = new byte[12];
		private byte[] newBytes = new byte[12];

		long lngSource_Base;
		long lngDestination_Base;

		int intSource_Base;
		int intDestination_Base;

		MethodInfo source;
		MethodInfo destination;

		public DetourHook(MethodInfo source, MethodInfo destination) {
			this.source = source;
			this.destination = destination;


			if (IntPtr.Size == sizeof(Int64)) {
				// 64-bit systems use 64-bit absolute address and jumps
				// 12 byte destructive

				// Get function pointers
				lngSource_Base = source.MethodHandle.GetFunctionPointer().ToInt64();
				lngDestination_Base = destination.MethodHandle.GetFunctionPointer().ToInt64();

				// Native source address
				Pointer_Raw_Source = (byte*)lngSource_Base;

				// Pointer to insert jump address into native code
				long* Pointer_Raw_Address = (long*)(Pointer_Raw_Source + 0x02);

				originalBytes[0] = *(Pointer_Raw_Source + 0x00);
				originalBytes[1] = *(Pointer_Raw_Source + 0x01);
				originalBytes[2] = *(Pointer_Raw_Source + 0x02);
				originalBytes[3] = *(Pointer_Raw_Source + 0x03);
				originalBytes[4] = *(Pointer_Raw_Source + 0x04);
				originalBytes[5] = *(Pointer_Raw_Source + 0x05);
				originalBytes[6] = *(Pointer_Raw_Source + 0x06);
				originalBytes[7] = *(Pointer_Raw_Source + 0x07);
				originalBytes[8] = *(Pointer_Raw_Source + 0x08);
				originalBytes[9] = *(Pointer_Raw_Source + 0x09);
				originalBytes[10] = *(Pointer_Raw_Source + 0x0A);
				originalBytes[11] = *(Pointer_Raw_Source + 0x0B);

				// Insert 64-bit absolute jump into native code (address in rax)
				// mov rax, immediate64
				// jmp [rax]
				*(Pointer_Raw_Source + 0x00) = 0x48;
				*(Pointer_Raw_Source + 0x01) = 0xB8;
				*Pointer_Raw_Address = lngDestination_Base; // ( Pointer_Raw_Source + 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 )
				*(Pointer_Raw_Source + 0x0A) = 0xFF;
				*(Pointer_Raw_Source + 0x0B) = 0xE0;

				newBytes[0] = *(Pointer_Raw_Source + 0x00);
				newBytes[1] = *(Pointer_Raw_Source + 0x01);
				newBytes[2] = *(Pointer_Raw_Source + 0x02);
				newBytes[3] = *(Pointer_Raw_Source + 0x03);
				newBytes[4] = *(Pointer_Raw_Source + 0x04);
				newBytes[5] = *(Pointer_Raw_Source + 0x05);
				newBytes[6] = *(Pointer_Raw_Source + 0x06);
				newBytes[7] = *(Pointer_Raw_Source + 0x07);
				newBytes[8] = *(Pointer_Raw_Source + 0x08);
				newBytes[9] = *(Pointer_Raw_Source + 0x09);
				newBytes[10] = *(Pointer_Raw_Source + 0x0A);
				newBytes[11] = *(Pointer_Raw_Source + 0x0B);

			} else {
				// 32-bit systems use 32-bit relative offset and jump
				// 5 byte destructive

				// Get function pointers
				intSource_Base = source.MethodHandle.GetFunctionPointer().ToInt32();
				intDestination_Base = destination.MethodHandle.GetFunctionPointer().ToInt32();

				// Native source address
				Pointer_Raw_Source = (byte*)intSource_Base;

				// Pointer to insert jump address into native code
				int* Pointer_Raw_Address = (int*)(Pointer_Raw_Source + 1);

				// Jump offset (less instruction size)
				int offset = (intDestination_Base - intSource_Base) - 5;

				// Insert 32-bit relative jump into native code
					

				originalBytes[0] = *(Pointer_Raw_Source + 0x00);
				originalBytes[1] = *(Pointer_Raw_Source + 0x01);
				originalBytes[2] = *(Pointer_Raw_Source + 0x02);
				originalBytes[3] = *(Pointer_Raw_Source + 0x03);
				originalBytes[4] = *(Pointer_Raw_Source + 0x04);

				*Pointer_Raw_Source = 0xE9;
				*Pointer_Raw_Address = offset;

				newBytes[0] = *(Pointer_Raw_Source + 0x00);
				newBytes[1] = *(Pointer_Raw_Source + 0x01);
				newBytes[2] = *(Pointer_Raw_Source + 0x02);
				newBytes[3] = *(Pointer_Raw_Source + 0x03);
				newBytes[4] = *(Pointer_Raw_Source + 0x04);
			}
		}

		public unsafe void install() {
			if (IntPtr.Size == sizeof(Int64)) {
				if (lngSource_Base != source.MethodHandle.GetFunctionPointer().ToInt32() 
					|| lngDestination_Base != destination.MethodHandle.GetFunctionPointer().ToInt32()) {
					throw new Exception("Coordinates changed! Aborting!!!");
				}

				*(Pointer_Raw_Source + 0x00) = newBytes[0];
				*(Pointer_Raw_Source + 0x01) = newBytes[1];
				*(Pointer_Raw_Source + 0x02) = newBytes[2];
				*(Pointer_Raw_Source + 0x03) = newBytes[3];
				*(Pointer_Raw_Source + 0x04) = newBytes[4];
				*(Pointer_Raw_Source + 0x05) = newBytes[5];
				*(Pointer_Raw_Source + 0x06) = newBytes[6];
				*(Pointer_Raw_Source + 0x07) = newBytes[7];
				*(Pointer_Raw_Source + 0x08) = newBytes[8];
				*(Pointer_Raw_Source + 0x09) = newBytes[9];
				*(Pointer_Raw_Source + 0x0A) = newBytes[10];
				*(Pointer_Raw_Source + 0x0B) = newBytes[11];
			} else {
				if (intSource_Base != source.MethodHandle.GetFunctionPointer().ToInt32()
					|| intDestination_Base != destination.MethodHandle.GetFunctionPointer().ToInt32()) {
					throw new Exception("Coordinates changed! Aborting!!!");
				}

				*(Pointer_Raw_Source + 0x00) = newBytes[0];
				*(Pointer_Raw_Source + 0x01) = newBytes[1];
				*(Pointer_Raw_Source + 0x02) = newBytes[2];
				*(Pointer_Raw_Source + 0x03) = newBytes[3];
				*(Pointer_Raw_Source + 0x04) = newBytes[4];
			}
		}

		public unsafe void uninstall() {
			if (IntPtr.Size == sizeof(Int64)) {
				if (lngSource_Base != source.MethodHandle.GetFunctionPointer().ToInt32()
					|| lngDestination_Base != destination.MethodHandle.GetFunctionPointer().ToInt32()) {
					throw new Exception("Coordinates changed! Aborting!!!");
				}
				*(Pointer_Raw_Source + 0x00) = originalBytes[0];
				*(Pointer_Raw_Source + 0x01) = originalBytes[1];
				*(Pointer_Raw_Source + 0x02) = originalBytes[2];
				*(Pointer_Raw_Source + 0x03) = originalBytes[3];
				*(Pointer_Raw_Source + 0x04) = originalBytes[4];
				*(Pointer_Raw_Source + 0x05) = originalBytes[5];
				*(Pointer_Raw_Source + 0x06) = originalBytes[6];
				*(Pointer_Raw_Source + 0x07) = originalBytes[7];
				*(Pointer_Raw_Source + 0x08) = originalBytes[8];
				*(Pointer_Raw_Source + 0x09) = originalBytes[9];
				*(Pointer_Raw_Source + 0x0A) = originalBytes[10];
				*(Pointer_Raw_Source + 0x0B) = originalBytes[11];
			} else {
				if (intSource_Base != source.MethodHandle.GetFunctionPointer().ToInt32()
					|| intDestination_Base != destination.MethodHandle.GetFunctionPointer().ToInt32()) {
					throw new Exception("Coordinates changed! Aborting!!!");
				}

				*(Pointer_Raw_Source + 0x00) = originalBytes[0];
				*(Pointer_Raw_Source + 0x01) = originalBytes[1];
				*(Pointer_Raw_Source + 0x02) = originalBytes[2];
				*(Pointer_Raw_Source + 0x03) = originalBytes[3];
				*(Pointer_Raw_Source + 0x04) = originalBytes[4];
			}
		}

		public unsafe object callOriginal(object obj, object[] parameters) {
			uninstall();
			try {
				return source.Invoke(obj, parameters);
			} finally {
				install();
			}
		}
	}
}