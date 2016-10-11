#include<string.h>    //strlen
#include<sys/socket.h>
#include<arpa/inet.h> //inet_addr
#include<unistd.h> 
#include "constants.h"


int handleNoMoreCmd(char * inputBuffer, int sock){
    return 0;
}

/*--------------------------------------------------------------
|  inputBuffer:
|   first 2 bytes of input buffer must contain command #
|   and 3rd byte should have args
*-------------------------------------------------------------------*/
int handleNullTerminatedCmd(char * inputBuffer, int sock){
	char * cmd = &inputBuffer[2];
    int sent_size;
   
    // Send some data
    // buffer size for send is strlen(inputBuffer) - 1,
    // to remove '\n' that is at the end of inputBuffer
    // cuz fgets
    if( (sent_size=(send(sock , inputBuffer , strlen(cmd) + sizeof(uint16_t) + 1 , 0))) < 0)
    {
        puts("Send failed");
        return -1;
    }

    printf("sent: %d\n", sent_size);

    return 1;
}

int handleGivenLengthCmd(char * inputBuffer, int sock){
    char * cmd = &inputBuffer[4];
   
    uint16_t cmdLen = strlen(cmd);
    printf("cmd len: %d\n", cmdLen);
    cmdLen = htons(cmdLen);
    memcpy(&inputBuffer[2], &cmdLen, sizeof(cmdLen));
   
    if( send(sock , inputBuffer , strlen(cmd) + sizeof(uint16_t) * 2 , 0) < 0)
    {
        puts("Send failed");
        return 1;
    }
}

int handleBadIntCmd(char * inputBuffer, int sock){
    char * cmd = &inputBuffer[2];
    char * c;
    
    // convert to integer
    int intCmd = strtol(cmd, &c, 10);

    // copy to send buffer without applying htonl
    memcpy(cmd, &intCmd, sizeof(intCmd));

    if(send(sock , inputBuffer ,sizeof(intCmd) + sizeof(uint16_t), 0) < 0)
    {
        puts("Send failed");
        return 1;
    }
}

int handleGoodIntCmd(char * inputBuffer, int sock){
    char * cmd = &inputBuffer[2];
    char * c;
    
    // convert to integer
    int intCmd = strtol(cmd, &c, 10);
    intCmd = htonl(intCmd);

    // copy to send buffer without applying htonl
    memcpy(cmd, &intCmd, sizeof(intCmd));

    if(send(sock , inputBuffer ,sizeof(intCmd) + sizeof(uint16_t), 0) < 0)
    {
        puts("Send failed");
        return 1;
    }
    return 1;
}

int handleKByteAtATimeCmd(char * inputBuffer, int sock){
    char * cmd = &inputBuffer[2];
    char * c;
    fflush(stdin);

    // convert to integer
    int k = strtol(cmd, &c, 10);
    // apply network ordering,
    // copy to send buffer and
    // send
    uint32_t tmp = htonl(k);
    memcpy(&inputBuffer[2], &tmp, sizeof(uint32_t));
    // send command # and nBytes
    
    // send command + arg
    send(sock, inputBuffer, sizeof(uint32_t) + sizeof(uint16_t), 0);
    
    int nBlocks = k / 1000;
    int remaining = k % 1000;
    int byteVal = 0;
    


    if((sendAlternateBinaryBytesInBlocks(sock, k, 1000) <= 0)){
        printf("Send failed\n");
        return -1;
    }
    return 1;
}

int handleByteAtATimeCmd(char * inputBuffer, int sock){
    char * cmd = &inputBuffer[2];
    char * c;
    fflush(stdin);

    // convert to integer
    int k = strtol(cmd, &c, 10);
    // apply network ordering,
    // copy to send buffer and
    // send
    uint32_t tmp = htonl(k);
    memcpy(&inputBuffer[2], &tmp, sizeof(uint32_t));
    // send command # and nBytes
    
    // send command + arg
    send(sock, inputBuffer, sizeof(uint32_t) + sizeof(uint16_t), 0);
    
    int nBlocks = k / 1000;
    int remaining = k % 1000;
    int byteVal = 0;
    


    if((sendAlternateBinaryBytesInBlocks(sock, k, 1) <= 0)){
        printf("Send failed\n");
        return -1;
    }
    return 1;
}   


int sendAlternateBinaryBytesInBlocks(int socket, int totalDataSize, int blockSize){
    char * data = (char *) malloc(blockSize);
    
    int nBlocks = totalDataSize / blockSize;
    int nBytesSent = 0;
    int currentVal = 0;

    while(nBytesSent < totalDataSize){
        memset(data, currentVal, blockSize);
        nBytesSent += send(socket, data, blockSize, 0);
        currentVal = !currentVal;
    }

    memset(data, currentVal, blockSize);
    nBytesSent += send(socket, data, totalDataSize - nBytesSent, 0);
    
    free(data);

    return nBytesSent;
}



int (*handlerPtrs[]) (char * , int) = {
                    handleNoMoreCmd,
                    handleNullTerminatedCmd,
                    handleGivenLengthCmd,
                    handleBadIntCmd,
                    handleGoodIntCmd,
                    handleByteAtATimeCmd,
                    handleKByteAtATimeCmd};


