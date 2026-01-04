topmost
최상단에 고정

screen
스크린 인덱스는 0번이면 첫번째 스크린
스크린 인덱스는 1번이면 두번째 스크린
듀얼모니터 전용옵션임

fit
이미지 너비가 스크린 너비를 벗어나지 않도록 한다.
즉, 다른 스크린 침범안하도록 하는 옵션임

width
이미지를 해당 너비로 고정 (fit 무시)
width만 설정할 경우 높이는 이미지 비율따라서 유지됨.

height
이미지를 해당 높이로 고정 (fit 무시)
height만 설정할 경우 높이는 이미지 비율따라서 유지됨.

실행 예시
./ImageViewer.exe "path=E:\Script\a.png,screen=1,x=100,y=200,width=600" "path=E:\Script\b.png,screen=1,x=200,y=200,fit=true"

끄는 법
Esc를 연속 입력한다.