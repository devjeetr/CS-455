#include <stdio.h>
#include <string.h>    //strlen
#include <sys/socket.h>
#include <arpa/inet.h> //inet_addr
#include <unistd.h>    //write
#include "constants.h"



uint16_t receiveShort(int socket){
	// Read command
	uint16_t input;

    int read_size = recv(socket, &input, sizeof(input), 0);
    if(read_size != sizeof(input)){
        printf("Invalid size received in receiveShort: %d\n", read_size);
    }

    return ntohs(input);
}