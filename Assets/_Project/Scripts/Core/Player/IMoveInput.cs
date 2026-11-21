using UnityEngine;

namespace FlyJam
{
    /// 이동 입력의 출처를 숨긴다. 키보드, 짝 AI, (나중에) 자이로·게임패드가 같은 인터페이스를 쓴다.
    public interface IMoveInput
    {
        Vector2 ReadMove();   // x = 좌우, y = 앞뒤, 크기 0~1
        bool ReadDodge();     // 이번 프레임에 회피 버튼을 눌렀는가
    }
}
