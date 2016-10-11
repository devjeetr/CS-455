/*
    C socket server example
*/
#include <stdio.h>
#include <string.h>    //strlen
#include <sys/socket.h>
#include <arpa/inet.h> //inet_addr
#include <unistd.h>    //write
#include "constants.h"
#include "utilities.c"
#include <unistd.h>
#include <signal.h>
#include "server_handlers.c"

#define MESSAGE_INDEX (2)



int socket_desc;

static volatile int keepRunning = 1;




void ctrCHandler(int dummy){

    //close file
    keepRunning = 0;

    printf("Saving log file and closing socket before exiting...\n");
    fclose(log_file);
    close(socket_desc);
    exit(0);
}

int main(int argc , char *argv[])
{
    int client_sock , c , read_size;
    struct sockaddr_in server , client;
    char client_message[DEFAULT_RECEIVE_SIZE];
    char response_message[DEFAULT_SEND_SIZE];

    signal(SIGINT, ctrCHandler);
    
    if(argc > 1){
        log_file = fopen(argv[1], "w+");
    }else{
        printf("log file not provided in args, using default: %s\n", DEFAULT_LOG_FILE);
        log_file = fopen(DEFAULT_LOG_FILE, "w+");
    }

    //Create socket
    socket_desc = socket(AF_INET , SOCK_STREAM , 0);
    if (socket_desc == -1)
    {
        printf("Could not create socket");
    }
    puts("Socket created");
     
    //Prepare the sockaddr_in structure
    server.sin_family = AF_INET;
    server.sin_addr.s_addr = INADDR_ANY;
    server.sin_port = htons( PORT_NUMBER );
     
    //Bind
    if( bind(socket_desc,(struct sockaddr *)&server , sizeof(server)) < 0)
    {
        //print the error message
        perror("bind failed. Error");
        return 1;
    }
    puts("bind done");
     
    //Listen
    listen(socket_desc , 3);
    
    while(keepRunning){ 
        //Accept and incoming connection
        puts("\n\nWaiting for incoming connections...\n");
        c = sizeof(struct sockaddr_in);

        //anccept connection from an incoming client
        client_sock = accept(socket_desc, (struct sockaddr *)&client, (socklen_t*)&c);
        if (client_sock < 0)
        {
            perror("accept failed");
            return 1;
        }

        printf("Connection Accepted:\n");
        printf("---------------------------------------------------------------------\n");

        

        while((read_size = loggedRecieve(log_file, client_sock, client_message, DEFAULT_RECEIVE_SIZE, 0)) > 0){
            totalBytesReceivedCurrentClient += read_size;
            // printf("Initial recv, received %d bytes\n", read_size);


            if(read_size == 0){
                puts("Empty recv. Closing connection");
                
            }else{

                    // extract command number
                uint16_t command;
                memcpy(&command, client_message, sizeof(command));
                command = ntohs(command);

                // check command is in valid range
                if(command < 0 && command >= NUMBER_OF_COMMANDS){
                    puts("Invalid command, terminating connection with client");
                    break;
                }

                printf("Command Received: %s\n", commandNames[command]);

                // Use function pointer table to call appropriate handler

                if((*handlerPtrs[command - 1])(response_message, client_message, client_sock, command, read_size) < 0){
                    printf("Error in handler. Breaking!\n");
                    break;
                }
            }
        }
        
        printf("Client connection closed. Total Bytes received: %d\n", totalBytesReceivedCurrentClient);
        printf("---------------------------------------------------------------------\n");

        totalBytesReceivedCurrentClient = 0;
        
            
    }
    fclose(log_file);
    close(socket);
    return 0;
}