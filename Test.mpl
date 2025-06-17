DemoModule := module()
    option package:

    export SayHi := proc(name::string)
        print(cat("Hi ", name, "!"))
    end proc:
end module: