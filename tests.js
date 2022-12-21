// File for dumping client unit tests, because JS is so unrealiable & hard to debug.

// 2D board array creation test
{
    let start = Date.now()

    let boards = [], boardsRows = 10, boardsColumns = 10

    for (let x = 0; x < boardsRows; x++) {
        boards[x] = []

        for (let y = 0; y < boardsColumns; y++) {
            boards[x][y] = "BOARD"
        }
    }

    console.log("BOARDS TEST -" + (boards[9][9] == "BOARD" ? "SUCCESS" : "FAIL")
                + " IN " + (Date.now() - start).toString() + "ms")
}

// Connect packet decoding + piece packet decode
{
    let start = Date.now()
    let connectBuffer = [0, 1, 1, 8, 8, 0, 0, 5, 7, 4, 0]

    let boardsRows, boardsColumns, pieceRows, pieceColumns
    [boardsRows, boardsColumns, pieceRows, pieceColumns] = connectBuffer.slice(1)
    
    let pieces = []
    let pieceArray = connectBuffer.slice(5)
    for (let i = 0; i < pieceArray.length; i += 6) {
        let select = pieceArray.slice(i, i + 6)
        pieces.push(select)
    }

    let success = true
    if (boardsRows != 1 || boardsColumns != 1 || pieceRows != 8 || pieceColumns != 8
        || pieces[0].toString() != connectBuffer.slice(5, 11).toString()) {
        success = false
    }

    console.log("CONNECT BUFFER TEST -" + (success ? "SUCCESS" : "FAIL")
                + " IN " + (Date.now() - start).toString() + "ms")
}

// Perform an ease out linear interpolation over 1600 frames (at 60fps) (setPosition)
{
    let initialLeft = -476
    let finalLeft = 87
    const repeats = 100
    let current = 0

    let animate = setInterval(() => {
        // We pretend offsetleft is always starting from zero, by shifting final back by it as well
        let finalShifted = finalLeft - initialLeft

        // We get progress of offsetleft over the total diff it must cover, 
        let leftShifted = board.offsetLeft - initialLeft

        //Where we should be at now (shifted)
        let xShiftedNow = easeOutCubic(current / repeats) * finalShifted

        // Where we actually should be now, removing shift
        let xNow = xShiftedNow + initialLeft
        
        current++
        if (current >= repeats) {
            clearInterval(animate)
            console.log("EASE OUT LERP TEST - " + (Math.floor(xNow) == finalLeft ? "SUCCESS" : "FAIL"))
        }
    }, 100)
}