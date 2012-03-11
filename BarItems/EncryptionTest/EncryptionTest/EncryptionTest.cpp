// EncryptionTest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

char key[] = "dicks";
char filepath[] = "C:\\Users\\Andrew\\test.txt";
char outFilePath[] = "C:\\Users\\Andrew\\test.txt";
char openMode[] = "rb";
char writeMode[] = "wb";
char fileData[10000];

int _tmain(int argc, _TCHAR* argv[])
{
	__asm{
		lea edi, filepath;
		lea ebx, openMode;

		push ebx;
		push edi;
		call dword ptr fopen;
		add esp, 8;
		or eax, eax;
		jnz fileOK;
		//error handling
fileOK:
		mov edi, eax;
		lea esi, fileData;
		lea ebx, key;
fileLoop:
		push edi;
		call dword ptr fgetc;
		add esp, 4;

		cmp al, EOF;
		je theEnd;

		//encrypt bit
		xor al, [ebx];
		inc ebx;
		cmp [ebx], 0;
		jne doNotLoopKey;
		lea ebx, key;

doNotLoopKey:
		
		mov [esi], al;
		inc esi;
		jmp fileLoop;


theEnd:
		mov [esi], 0;
		push edi;
		call dword ptr fclose;
		add esp, 4;

		lea edi, outFilePath;
		lea ebx, writeMode;
		push ebx;
		push edi;
		call dword ptr fopen;
		add esp, 8;
		or eax, eax;
		jnz writeFileOK;
		//error handling

writeFileOK:
		mov edi, eax;
		lea esi, fileData;
		push esi;
		push edi;
		call dword ptr fprintf;
		add esp, 8;

		push edi;
		call dword ptr fclose;
		add esp, 4;






	}
	return 0;
}

