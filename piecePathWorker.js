// Worker intended to load up piece paths in the background as the site is started,
// without causing freezing or disruption to active game

async function beginLoad() {
    for (let name of ["bishop", "king", "knight", "pawn", "queen", "rook"]) {
        let res = await (await fetch("./Assets/" + name + ".svg")).text()
        postMessage(res)   
    }
}

beginLoad()