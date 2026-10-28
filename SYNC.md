# Welcome Korea Git 동기화

관리 폴더: C:\Users\User\Desktop\github\settest
GitHub: https://github.com/kim-81/settest
기준 브랜치: codex/welcome-korea-v1.0
배포 폴더: dist/v1.0

배포 폴더명 v1.0은 사용자가 정리한 이름이며 내부 앱 버전은 최신 1.8입니다.
이전 배포 폴더는 복원하지 않습니다. dist 바로 아래의 중복 실행 파일은 업로드에서 제외합니다.
현재 소스와 실행/설치 파일은 같은 저장소에 포함합니다. PC별 사용자 설정은 포함하지 않습니다.

변경을 시작하기 전에 이 폴더에서 git status를 확인합니다.
로컬 변경이 없으면 git pull --ff-only로 원격 변경을 받습니다.
프로그램 빌드는 powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1로 수행합니다.
테스트가 통과하면 dist/v1.0의 실행/설치 파일과 SHA256SUMS.txt도 갱신됩니다.
검토 후 git add, git commit, git push로 업로드합니다.
원격 변경과 충돌하면 강제 푸시하지 않고 내용을 확인해 해결합니다.

실시간 자동 업로드나 예약 작업은 설정하지 않았습니다.
앞으로 사용자가 변경 또는 동기화를 요청할 때 이 폴더와 연결 저장소를 기준으로 작업합니다.
