echo Building.....
./gradlew assemble
echo Build successfull

mv build/libs/Project\ 2\ Routing.jar TestBed/app.jar

jarFile="TestBed/app.jar"
echo $jarFile
gnome-terminal -e "java -jar $jarFile TestBed/test1 A"
gnome-terminal -e "java -jar $jarFile TestBed/test1 C"
gnome-terminal -e "java -jar $jarFile TestBed/test1 B"
gnome-terminal -e "java -jar $jarFile TestBed/test1 E"
gnome-terminal -e "java -jar $jarFile TestBed/test1 D"
