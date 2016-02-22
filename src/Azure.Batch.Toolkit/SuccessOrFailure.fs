namespace Batch.Toolkit

type internal SuccessOrFailure<'a>  = 
| Success of 'a
| Failure of exn
with
    static member Return x = Success x
    static member Zero () = Success
    member this.Bind(f) =
        match this with 
        | Success x -> 
            try 
                f x
            with ex -> Failure ex
        | Failure x -> Failure x
    member this.Map(f) =
        this.Bind (fun x -> f x |> Success) 

[<AutoOpen>]
module internal SuccessOrFailure =
    let internal (>>=) (m : SuccessOrFailure<_>) f = m.Bind (f)
    let internal (<!>) (m : SuccessOrFailure<_>) f = m.Map (f)
    let internal point = SuccessOrFailure<_>.Return
    let internal getOrThrow = function
    | Success x -> x
    | Failure x -> raise x

    type SuccessOrFailureBuilder () =
        member this.Bind (m, f) = m >>= f
        member this.Return v = point v
        member this.Yield v = point v
        member this.ReturnFrom m = m
        member this.YieldFrom m = m
        member this.Zero() = SuccessOrFailure<_>.Zero

    let foldrM f z = List.fold (fun m a -> m >>= f a) (point z)

    let succeed = new SuccessOrFailureBuilder ()
