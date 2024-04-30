export function getWindowSize() {
    return {
        Width: window.innerWidth,
        Height: window.innerHeight
    }
}

export function registerServiceUpdates(dotnetHelper) {
    window.addEventListener('load', () => {   
        dotnetHelper.invokeMethodAsync('ServiceWindowSizeUpdate', window.innerWidth, window.innerHeight);    
    })
    window.addEventListener('resize', () => {
        dotnetHelper.invokeMethodAsync('ServiceWindowSizeUpdate', window.innerWidth, window.innerHeight)
    })

    // Touch screen doesn't work well on mobile device if the event listener is attached to the window.
    // Instead it's attached to the document.
    window.document.addEventListener('touchstart', (event) => {
        var array = new Array(event.touches.length)              
        for (var i = 0; i < event.touches.length; i++) {
            let touch = event.touches[i];
            array[i] = {
                X: touch.pageX,
                Y: touch.pageY
            }            
        }        
        dotnetHelper.invokeMethodAsync('ServiceTouchStartUpdate', array)
    })

    window.addEventListener('click', (event) => {
        dotnetHelper.invokeMethodAsync('ServiceKeyPressedUpdate', event.key, true)
    })

    window.addEventListener('keydown', (event) => {     
        dotnetHelper.invokeMethodAsync('ServiceKeyPressedUpdate', event.key, true)
    })
    window.addEventListener('keyup', (event) => {
        dotnetHelper.invokeMethodAsync('ServiceKeyPressedUpdate', event.key, false)
    })

    window.addEventListener('blur', (event) => {        
        dotnetHelper.invokeMethodAsync('ServiceKeyPressedUpdate', null, false)
    })
}