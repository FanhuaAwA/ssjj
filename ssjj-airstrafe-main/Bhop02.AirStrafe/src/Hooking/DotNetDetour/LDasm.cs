using System;

namespace Bhop02.DotNetDetour;

internal class LDasm
{
	private struct ldasm_data
	{
		public byte flags;

		public byte rex;

		public byte modrm;

		public byte sib;

		public byte opcd_offset;

		public byte opcd_size;

		public byte disp_offset;

		public byte disp_size;

		public byte imm_offset;

		public byte imm_size;
	}

	private const int F_INVALID = 1;

	private const int F_PREFIX = 2;

	private const int F_REX = 4;

	private const int F_MODRM = 8;

	private const int F_SIB = 16;

	private const int F_DISP = 32;

	private const int F_IMM = 64;

	private const int F_RELATIVE = 128;

	private const int OP_NONE = 0;

	private const int OP_INVALID = 128;

	private const int OP_DATA_I8 = 1;

	private const int OP_DATA_I16 = 2;

	private const int OP_DATA_I16_I32 = 4;

	private const int OP_DATA_I16_I32_I64 = 8;

	private const int OP_EXTENDED = 16;

	private const int OP_RELATIVE = 32;

	private const int OP_MODRM = 64;

	private const int OP_PREFIX = 128;

	private static readonly byte[] flags_table = new byte[256]
	{
		64, 64, 64, 64, 1, 4, 0, 0, 64, 64,
		64, 64, 1, 4, 0, 0, 64, 64, 64, 64,
		1, 4, 0, 0, 64, 64, 64, 64, 1, 4,
		0, 0, 64, 64, 64, 64, 1, 4, 128, 0,
		64, 64, 64, 64, 1, 4, 128, 0, 64, 64,
		64, 64, 1, 4, 128, 0, 64, 64, 64, 64,
		1, 4, 128, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 64, 64,
		128, 128, 128, 128, 4, 68, 1, 65, 0, 0,
		0, 0, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 65, 68,
		65, 65, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 6, 0, 0, 0, 0, 0,
		1, 8, 1, 8, 0, 0, 0, 0, 1, 4,
		0, 0, 0, 0, 0, 0, 1, 1, 1, 1,
		1, 1, 1, 1, 8, 8, 8, 8, 8, 8,
		8, 8, 65, 65, 2, 0, 64, 64, 65, 68,
		3, 0, 2, 0, 0, 1, 0, 0, 64, 64,
		64, 64, 1, 1, 0, 0, 64, 64, 64, 64,
		64, 64, 64, 64, 33, 33, 33, 33, 1, 1,
		1, 1, 36, 36, 6, 33, 0, 0, 0, 0,
		128, 0, 128, 128, 0, 0, 64, 64, 0, 0,
		0, 0, 0, 0, 64, 64
	};

	private static readonly byte[] flags_table_ex = new byte[256]
	{
		64, 64, 64, 64, 128, 0, 0, 0, 0, 0,
		128, 0, 128, 64, 128, 65, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 128, 128, 128, 128, 128,
		128, 0, 64, 64, 64, 64, 80, 128, 64, 128,
		64, 64, 64, 64, 64, 64, 64, 64, 0, 0,
		0, 0, 0, 0, 128, 0, 80, 128, 81, 128,
		128, 128, 128, 128, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 65, 65, 65, 65, 64, 64, 64, 0,
		64, 64, 128, 128, 64, 64, 64, 64, 36, 36,
		36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
		36, 36, 36, 36, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		0, 0, 0, 64, 65, 64, 128, 128, 0, 0,
		0, 64, 65, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 65, 64, 64, 64,
		64, 64, 64, 64, 65, 64, 65, 65, 65, 64,
		0, 0, 0, 0, 0, 0, 0, 0, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 128
	};

	private static byte cflags(byte op)
	{
		return flags_table[op];
	}

	private static byte cflags_ex(byte op)
	{
		return flags_table_ex[op];
	}

	public unsafe static uint SizeofMinNumByte(void* code, int size)
	{
		uint num = 0u;
		ldasm_data ld = default(ldasm_data);
		bool @is = IntPtr.Size == 8;
		uint num2;
		do
		{
			num2 = ldasm(code, ld, @is);
			byte* ptr = (byte*)code + (int)ld.opcd_offset;
			num += num2;
			if ((ulong)num >= (ulong)size || (num2 == 1 && *ptr == 204))
			{
				break;
			}
			code = (byte*)code + num2;
		}
		while (num2 != 0);
		return num;
	}

	private unsafe static uint ldasm(void* code, ldasm_data ld, bool is64)
	{
		byte* ptr = (byte*)code;
		byte b3;
		byte b4;
		byte b2;
		byte b = (b2 = (b3 = (b4 = 0)));
		if (code == null)
		{
			return 0u;
		}
		while ((cflags(*ptr) & 0x80) != 0)
		{
			if (*ptr == 102)
			{
				b3 = 1;
			}
			if (*ptr == 103)
			{
				b4 = 1;
			}
			ptr++;
			b2++;
			ld.flags |= 2;
			if (b2 == 15)
			{
				ld.flags |= 1;
				return b2;
			}
		}
		if (is64 && *ptr >> 4 == 4)
		{
			ld.rex = *ptr;
			b = (byte)((ld.rex >> 3) & 1);
			ld.flags |= 4;
			ptr++;
			b2++;
		}
		if (is64 && *ptr >> 4 == 4)
		{
			ld.flags |= 1;
			return (uint)(b2 + 1);
		}
		ld.opcd_offset = (byte)(ptr - (byte*)code);
		ld.opcd_size = 1;
		byte b5 = *(ptr++);
		b2++;
		byte b6;
		if (b5 == 15)
		{
			b5 = *(ptr++);
			b2++;
			ld.opcd_size++;
			b6 = cflags_ex(b5);
			if ((b6 & 0x80) != 0)
			{
				ld.flags |= 1;
				return b2;
			}
			if ((b6 & 0x10) != 0)
			{
				b5 = *(ptr++);
				b2++;
				ld.opcd_size++;
			}
		}
		else
		{
			b6 = cflags(b5);
			if (b5 >= 160 && b5 <= 163)
			{
				b3 = b4;
			}
		}
		if ((b6 & 0x40) != 0)
		{
			byte b7 = (byte)(*ptr >> 6);
			byte b8 = (byte)((*ptr & 0x38) >> 3);
			byte b9 = (byte)(*ptr & 7);
			ld.modrm = *(ptr++);
			b2++;
			ld.flags |= 8;
			if (b5 == 246 && (b8 == 0 || b8 == 1))
			{
				b6 |= 1;
			}
			if (b5 == 247 && (b8 == 0 || b8 == 1))
			{
				b6 |= 8;
			}
			if (b7 != 3 && b9 == 4 && (is64 || b4 == 0))
			{
				ld.sib = *(ptr++);
				b2++;
				ld.flags |= 16;
				if ((ld.sib & 7) == 5 && b7 == 0)
				{
					ld.disp_size = 4;
				}
			}
			switch (b7)
			{
			case 0:
				if (is64)
				{
					if (b9 == 5)
					{
						ld.disp_size = 4;
						if (is64)
						{
							ld.flags |= 128;
						}
					}
				}
				else if (b4 != 0)
				{
					if (b9 == 6)
					{
						ld.disp_size = 2;
					}
				}
				else if (b9 == 5)
				{
					ld.disp_size = 4;
				}
				break;
			case 1:
				ld.disp_size = 1;
				break;
			case 2:
				if (is64)
				{
					ld.disp_size = 4;
				}
				else if (b4 != 0)
				{
					ld.disp_size = 2;
				}
				else
				{
					ld.disp_size = 4;
				}
				break;
			}
			if (ld.disp_size > 0)
			{
				ld.disp_offset = (byte)(ptr - (byte*)code);
				ptr += (int)ld.disp_size;
				b2 += ld.disp_size;
				ld.flags |= 32;
			}
		}
		if (b != 0 && (b6 & 8) != 0)
		{
			ld.imm_size = 8;
		}
		else if ((b6 & 4) != 0 || (b6 & 8) != 0)
		{
			ld.imm_size = (byte)(4 - (b3 << 1));
		}
		ld.imm_size += (byte)(b6 & 3);
		if (ld.imm_size != 0)
		{
			b2 += ld.imm_size;
			ld.imm_offset = (byte)(ptr - (byte*)code);
			ld.flags |= 64;
			if ((b6 & 0x20) != 0)
			{
				ld.flags |= 128;
			}
		}
		if (b2 > 15)
		{
			ld.flags |= 1;
		}
		return b2;
	}
}

