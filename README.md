# AnarchyChess
A massive multiplayer chess board, with multiple gamemodes and slightly bent rules. Here be anarchy.

![image](https://user-images.githubusercontent.com/73035340/208559754-7d6b4d88-53ae-418a-ab85-bf68489546b0.png)

### Gamemodes:
 - Anarchy Chess: A massive multiplayer chess board, white vs black deathmatch, earn points and take pieces.
 - Situation: Classic chess, except every 30 seconds every pair's board is swapped for another, leaving them in an entierly new situation. First person to checkmate in the pair wins.
 - Classic: Bog standard chess matchmaking with no catches. Play against an opponent across the world.
 - Battle Royal: Everyone's given a unique colour: there are no teammates. Kill to survive, land on piece upgrades to promote to a higher (or lower) piece. Collect lives and other boosts. First to kill the king who will spawn after enough players are eliminated wins.

### Developing:
The frontend client of the game is develped using Typescript + HTML. For this, I have created the tool [InlineScript (tshtml)](https://github.com/Zekiah-A/InlineScript) that allows you to transpile the site's custom *.tshtml format into a normal Javascript + HTML file that may be used within the browser. Do not directly edit any .HTML file, as updates to this file will not be persisted when the site is built, instead modify the .tshtml equivalent to make modifications to the site's codebase.  

### Credits:
All credits for SVG pieces goes to nikfrank. Assets sourced from https://github.com/nikfrank/react-chess-pieces.
