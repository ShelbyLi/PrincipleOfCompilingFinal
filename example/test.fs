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

// let list = []

// let res =
//     let a = list @ [1; 2; 3]
//     [-1] @ a

let  f = 1.75
let bytes = System.BitConverter.GetBytes(float32(f))
bytes
let v = System.BitConverter.ToInt32(bytes, 0)

let bytes1 = System.BitConverter.GetBytes(int32(v))
let v1 = System.BitConverter.ToSingle(bytes1, 0)

let res = int(round(v1))