echo Building.....
gradlew assemble
echo Build successfull

mv build/libs/Project\ 2\ Routing.jar TestBed/app.jar

jarFile="TestBed/app.jar"
echo $jarFile
gnome-terminal -e "java -jar $jarFile TestBed/test2 A"
gnome-terminal -e "java -jar $jarFile TestBed/test2 C"
gnome-terminal -e "java -jar $jarFile TestBed/test2 B"
gnome-terminal -e "java -jar $jarFile TestBed/test2 E"
gnome-terminal -e "java -jar $jarFile TestBed/test2 D"
gnome-terminal -e "java -jar $jarFile TestBed/test2 G"
gnome-terminal -e "java -jar $jarFile TestBed/test2 F"
gnome-terminal -e "java -jar $jarFile TestBed/test2 H"
