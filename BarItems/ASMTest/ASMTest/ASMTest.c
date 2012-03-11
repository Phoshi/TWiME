// ASMTest.c : Defines the entry point for the console application.
//



int _tmain(int argc, char* argv[])
{
	__asm {
		MOV EAX,17;
		MOV EBX,19;
		ADD EAX,EBX;
	}
	return 0;
}

