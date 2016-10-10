/*
    C ECHO client example using sockets
*/
#include <stdio.h> //printf
#include <string.h>    //strlen
#include <sys/socket.h>    //socket
#include <arpa/inet.h> //inet_addr
#include "constants.h"


int readInt(){
    int inputBuffer[30];
    char * c;
    memset(inputBuffer, 0, 30);
    fgets(inputBuffer, 30, stdin);

    int n = strtol(inputBuffer, &c, 10);

    return n;
}


int getCommand(){
    int choice = -1;

    while(1){
        fflush(stdin);
        // print menu
        printf("Command Menu:\n");
        for(int i = 0; i < NUMBER_OF_COMMANDS; i++){
            printf("%d. %s\n", i + 1, commandNames[i]);
        }
        printf("Enter choice: ");
        choice = readInt();

        if(choice > 0 && choice <= NUMBER_OF_COMMANDS )
            return --choice;
        else
            printf("Invalid choice. Please try again.");
    }
}


int main(int argc , char *argv[])
{
    int sock;
    struct sockaddr_in server;
    char message[DEFAULT_SEND_SIZE] , server_reply[DEFAULT_RECEIVE_SIZE];
     
    //Create socket
    sock = socket(AF_INET , SOCK_STREAM , 0);
    if (sock == -1)
    {
        printf("Could not create socket");
    }
    puts("Socket created");
     
    server.sin_addr.s_addr = inet_addr("127.0.0.1");
    server.sin_family = AF_INET;
    server.sin_port = htons( PORT_NUMBER );
 
    //Connect to remote server
    if (connect(sock , (struct sockaddr *)&server , sizeof(server)) < 0)
    {
        perror("connect failed. Error");
        return 1;
    }
     
    puts("Connected\n");
    char inputBuffer[INPUT_BUFFER_SIZE];

    while(1){
        memset(inputBuffer, 0, INPUT_BUFFER_SIZE);

        uint16_t choice = getCommand(); 
        printf("Choice: %d\n", choice);
        
        if(choice == noMoreCommands){
            printf("Exiting....\n");
            break;
        }else {
            uint16_t tmp = htons(choice);
            // copy command # to input buffer
            memcpy(inputBuffer, &tmp, sizeof(tmp));

            if(choice == nullTerminatedCmd){
                    char * cmd = &inputBuffer[2];

                    fflush(stdin);

                    printf("Please enter command: ");
                    fgets(cmd, INPUT_BUFFER_SIZE, stdin);
                    cmd[strlen(cmd) - 1] = 0;
                    // printf("Sending message of length: %d\n", strlen(inputBuffer));
        
                    // Send some data
                    // buffer size for send is strlen(inputBuffer) - 1,
                    // to remove '\n' that is at the end of inputBuffer
                    // cuz fgets
                    
                    if( send(sock , inputBuffer , strlen(cmd) + sizeof(tmp) + 1 , 0) < 0)
                    {
                        puts("Send failed");
                        return 1;
                    }
                    
                } else if(choice == givenLengthCmd){
                    

                    char * cmd = &inputBuffer[4];
                    fflush(stdin);
                    printf("Please enter command: ");
                    fgets(cmd, INPUT_BUFFER_SIZE, stdin);
                    cmd[strlen(cmd) - 1] = 0;
                    printf("input: %s\n", cmd);

                    uint16_t cmdLen = strlen(cmd);
                    cmdLen = htons(cmdLen);
                    memcpy(&inputBuffer[2], &cmdLen, sizeof(cmdLen));
                    // printf("Sending message of length: %d\n", strlen(inputBuffer));
        

                    // Send some data
                    // buffer size for send is strlen(inputBuffer) - 1,
                    // to remove '\n' that is at the end of inputBuffer
                    // cuz fgets
                    if( send(sock , inputBuffer , strlen(cmd) + sizeof(uint16_t) * 2 , 0) < 0)
                    {
                        puts("Send failed");
                        return 1;
                    }
                } else if(choice == goodIntCmd || choice == badIntCmd){
                    char * cmd = &inputBuffer[2];
                    char * c;
                    fflush(stdin);

                    // input int command from user
                    printf("Please enter command: ");
                    fgets(cmd, INPUT_BUFFER_SIZE, stdin);
                    cmd[strlen(cmd) - 1] = 0;

                    // convert to integer
                    int intCmd = strtol(cmd, &c, 10);

                    printf("intCmd: %d\n", intCmd );
                    
                    // apply network ordering and copy
                    // to send buffer
                    if(choice == goodIntCmd )
                        intCmd = htonl(intCmd);

                    memcpy(cmd, &intCmd, sizeof(intCmd));

                    // printf("Sending message of length: %d\n", strlen(inputBuffer));
        
                    // Send some data
                    // buffer size for send is strlen(inputBuffer) - 1,
                    // to remove '\n' that is at the end of inputBuffer
                    // cuz fgets
                    
                    if( send(sock , inputBuffer ,sizeof(intCmd) + sizeof(uint16_t), 0) < 0)
                    {
                        puts("Send failed");
                        return 1;
                    }

                } else if(choice == kByteAtATimeCmd || choice == byteAtATimeCmd){
                    char * cmd = &inputBuffer[2];
                    char * c;
                    fflush(stdin);

                    // input int command from user
                    printf("Please n: ");
                    fgets(cmd, INPUT_BUFFER_SIZE, stdin);
                    cmd[strlen(cmd) - 1] = 0;

                    // convert to integer
                    int k = strtol(cmd, &c, 10);
                    // apply network ordering,
                    // copy to send buffer and
                    // send
                    uint32_t tmp = htonl(k);
                    memcpy(&inputBuffer[2], &tmp, sizeof(uint32_t));
                    // send command # and nBytes
                    
                    printf("k: %d\n", k );
                    

                    // apply nBlocksetwork ordering and copy
                    // to send buffer
                    
                   
                    
                    // send command + nBytes
                    send(sock, inputBuffer, sizeof(uint32_t) + sizeof(uint16_t), 0);
                    
                    int nBlocks = k / 1000;
                    int remaining = k % 1000;
                    int byteVal = 0;
                    
                    for(int i = 0; i < nBlocks; i++){
                        memset(&inputBuffer[1000 * i], byteVal, 1000);
                        byteVal = !byteVal;
                    }
                    memset(&inputBuffer[1000 * nBlocks], byteVal, remaining);


                    if(choice == byteAtATimeCmd){
                        sendInKBlocks(sock, inputBuffer, k, 1);
                    }else{
                        sendInKBlocks(sock, inputBuffer, k, 1000);
                    }
                    
                }
        }
        printf("Waitinf for server reply\n");
        //Receive a reply from the server
        if( recv(sock , server_reply , DEFAULT_RECEIVE_SIZE , 0) < 0)
        {
            puts("recv failed");
            break;
        }
         
        puts("Server reply :");

        // first 2 bytes is length
        uint16_t len;
        memcpy(&len, server_reply, sizeof(uint16_t));
        len = ntohs(len);
        printf("%d bytes: %s\n", len, server_reply + sizeof(uint16_t));
        printf("\n");



   
    }

    close(sock);
    return 0;
}


void sendInKBlocks(int socket, char * data, int totalDataSize, int blockSize){
    int nBlocks = totalDataSize / blockSize;

    while(nBlocks-- > 0){
        totalDataSize -= send(socket, data++, blockSize, 0);
    }

    totalDataSize -= send(socket, data, totalDataSize, 0);

}