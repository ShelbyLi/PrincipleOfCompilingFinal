// let sa = @"hello %d dude %d"
// evals = [1; 2; 3]
// let slist = sa.Split('%')
// let mutable resString = slist.[0]
// let mutable i = 1
// while i < slist.Length do
//     resString <- resString + evals.[i-1].ToString() + slist.[i]
//     printf "%s\n" resString
//     i <- i + 1
// printf "%s" "resString".[1..]

let list = []

let res =
    let a = list @ [1; 2; 3]
    [-1] @ a
