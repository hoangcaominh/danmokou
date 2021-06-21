﻿using System;
using Danmokou.Core;
using Danmokou.Services;
using Suzunoya.Entities;
using SuzunoyaUnity.Derived;
using SuzunoyaUnity.Mimics;
using UnityEngine;

namespace MiniProjects.VN {
public class Mokou : SZYUCharacter {
    public override Color TextColor => new Color(.97f, .75f, .864f);
    public override Color UIColor => new Color(.63f, .03f, .14f);
    public override string Name => "Fujiwara no Mokou";
    
    public override void RollEvent() => DependencyInjection.SFXService.Request("x-bubble-2", SFXType.TypingSound);
}

public class MokouMimic : PiecewiseCharacterMimic {
    public override Type[] CoreTypes => new[] {typeof(Mokou)};
}

}