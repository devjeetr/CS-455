echo Building.....
./gradlew assemble
echo Build successfull

mv build/libs/Project\ 2\ Routing.jar TestBed/app.jar

jarFile="TestBed/app.jar"
echo $jarFile
gnome-terminal -e "java -jar $jarFile TestBed/test4 A"
gnome-terminal -e "java -jar $jarFile TestBed/test4 C"
gnome-terminal -e "java -jar $jarFile TestBed/test4 B"
gnome-terminal -e "java -jar $jarFile TestBed/test4 E"
gnome-terminal -e "java -jar $jarFile TestBed/test4 D"
gnome-terminal -e "java -jar $jarFile TestBed/test4 G"
gnome-terminal -e "java -jar $jarFile TestBed/test4 F"
gnome-terminal -e "java -jar $jarFile TestBed/test4 H"
