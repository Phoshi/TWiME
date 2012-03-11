// ReadFile.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

char readMode[] = "rb";
char fileFail[] = "oh god everything is dying help me oh god they're coming";
int p = 17; //two
int q = 11; //primes
int n = p * q; //their product
int e = 7; //and the e

int d = 23; //and the e for decrypting


int _tmain(int argc, _TCHAR* argv[])
{
	char theString[50] = "c:\\users\\andrew\\test.txt";
	char theFile[10000];
	//printf("Give me a filepath!\n");
	__asm{
		jmp fileOpen;
		lea ebx, theString; //Load the address of the first index of our array

getNextCharacter:
		push ebx; //call getchar without breaking our registers
		call dword ptr getchar;
		pop ebx;

		mov [ebx], al; //stick the read character into our array index
		inc ebx; //and move along one

		cmp al,'\n'; //if the character was a newline, we're done here
		jnz getNextCharacter; //otherwise, we aren't
		dec ebx; //if we're done, pop back an index
		mov [ebx], 0 //and add a null. Bam, done.

fileOpen:
		lea eax, readMode; //load read mode
		push eax;

		lea eax, theString; //load file path
		push eax;
		call dword ptr fopen; //open the file
		add esp, 8;

		or eax, eax; //if handle isn't zero, we win
		jnz fileOK;

		lea eax, fileFail; //else die
		push eax;
		call dword ptr printf;
		add esp, 4;
		jmp killMeNow;

fileOK:
		lea edi, theFile; //load the file data pointer to edi
		mov ebx, eax; //move the file handle to backup
readMore:
		push ebx;
			call dword ptr fgetc; //grab a character
		pop ebx;

		push ebx;
		//
		//encrypt it
		//

		push al;
			mov ecx, e;
			mov edx, eax;
	pow: //data ** e
			push edx;
				mul edx;
			pop edx;
			loop pow;

			//(data ** e)%(n)
			mov ebx, n;
			sub edx, edx; //have to clear out edx for... some reason
			div ebx;
			mov eax, edx; //edx stores the remainder - the mod

			//
			//end encryption
			//

			//
			//decrypt it
			//

			//d is way too big. Like crazy big.
			mov esi, d;
			mov ebx, 0;
unRSALoop:
			mov ecx, 5;
			cmp ecx, esi;
			jge biggerThan;
			jmp otherwise;
biggerThan:
			mov ecx, esi;
			mov esi, 0;
			jmp endThan;
otherwise:
			sub esi, ecx;
			mov edx, eax;
endThan:
			push esi;
powTwo: //data ** d
			push edx;
				mul edx;
			pop edx;
			loop powTwo;

			//(data ** d)%(n)
			push ebx; //still has our file handle in it
				mov ebx, n;
				sub edx, edx; //have to clear out edx for... some reason
				div ebx;
				mov eax, edx; //edx stores the remainder - the mod
			pop ebx;

			cmp ebx, 0;
			jz runOnce;
			jnz runMult
runOnce:
			mov ebx, eax;
			jmp runEnd;

runMult:
			mul ebx;
			mov ebx, eax;
			jmp runEnd;

runEnd:
			pop esi;
			cmp esi, 0;
			jnz unRSALoop;


			mov [edi], al; //add it in
			inc edi;

		pop al;
		cmp al, EOF; //EOF maybe? //if we're at the end of the file, jump out
		pop ebx;
		jz theEnd;

		jmp readMore; //else don't

theEnd:
		mov [edi], 0; //null terminate the bloody string
		push ebx;
		call dword ptr fclose; //close the file
		add esp, 4;

		lea eax, theFile;
		push eax;
		call dword ptr printf; //print it out
		add esp, 4;

killMeNow:
		call dword ptr getchar; //wait for input before quitting
		




	}
	return 0;
}

