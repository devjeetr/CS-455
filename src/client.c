/*
    C ECHO client example using sockets
*/
#include <stdio.h> //printf
#include <string.h>    //strlen
#include <sys/socket.h>    //socket
#include <arpa/inet.h> //inet_addr
#include "constants.h"
#include "client_handlers.c"


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

// TODO: 
//  Add command processing loop
//

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

    int bufferCounter = 0;



    for(int i = 0 ; i < 7; i++){
        memset(inputBuffer, 0, INPUT_BUFFER_SIZE);
        printf("command: %d\n", commands[i].cmd);

        if(commands[i].cmd == 0){
            printf("Exiting....\n");
            break;
        }
        

        
        uint16_t tmp = htons(commands[i].cmd);
        
        // copy copyommand # to input buffer
        memcpy(inputBuffer, &tmp, sizeof(uint16_t));

        
        //copy arg
        memcpy(inputBuffer + sizeof(uint16_t), commands[i].arg, strlen(commands[i].arg));
        // call handler from handler table
        if((*handlerPtrs[commands[i].cmd])(inputBuffer, sock) < 0){
            printf("Error in handler. Breaking!\n");
            break;
        }

        printf("Waiting for server reply\n");
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

    // while(1){
        // memset(inputBuffer, 0, INPUT_BUFFER_SIZE);

        // uint16_t choice = getCommand(); 
        // printf("Choice: %d\n", choice);
        
        // if(choice == noMoreCommands){
        //     printf("Exiting....\n");
        //     break;
        // }else {
        //     uint16_t tmp = htons(choice);
            
        //     // copy command # to input buffer
        //     memcpy(inputBuffer, &tmp, sizeof(uint16_t));
        //     char * cmd = inputBuffer + sizeof(uint16_t);
        //     printf("Please enter arg: ");
            
        //     // special case for givenLengthCmd
        //     if(choice == givenLengthCmd){
        //         fgets(cmd + 2, INPUT_BUFFER_SIZE, stdin);
        //         cmd[strlen(cmd + 2) + 1] = 0;
        //     }else{
        //         fgets(cmd, INPUT_BUFFER_SIZE, stdin);
        //         cmd[strlen(cmd) - 1] = 0;
        //     }
             
            // if((*handlerPtrs[choice])(inputBuffer, sock) < 0){
            //     printf("Error in handler. Breaking!\n");
            //     break;
            // }
    //     }
        // printf("Waitinf for server reply\n");
        // //Receive a reply from the server
        // if( recv(sock , server_reply , DEFAULT_RECEIVE_SIZE , 0) < 0)
        // {
        //     puts("recv failed");
        //     break;
        // }
         
        // puts("Server reply :");

        // // first 2 bytes is length
        // uint16_t len;
        // memcpy(&len, server_reply, sizeof(uint16_t));
        // len = ntohs(len);
        // printf("%d bytes: %s\n", len, server_reply + sizeof(uint16_t));
        // printf("\n");
    // }

    close(sock);
    return 0;
}

