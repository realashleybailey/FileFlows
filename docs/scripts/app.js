document.addEventListener("DOMContentLoaded", function()
{
    var checkboxes = document.querySelectorAll('.side-bar input[type=checkbox]');
    for(let chk of checkboxes){
        checkToggle(chk);
    }
    addCopyCodeButton();
});

function checkToggle(chk)
{
    let id = chk.id;
    if(localStorage.getItem('collapse_' + id) == true){
        chk.checked = true;
        console.log('toggle3:', id);
        console.log('collapse3: ', localStorage.getItem('collapse_' + id));

        let parent = chk.parentNode.closest('input[type=checkbox]');
        if(parent)
            checkToggle(parent);
    }
}

function toggleCollapse(event){
    let id = event.target.id;
    let chk = event.target;
    console.log('chk.id: ', chk.id);
    console.log('chk.checked: ', chk.checked);
    localStorage.setItem('collapse_' + id, chk.checked ? 1 : 0);

    // close any below this one if checked
    if(event.target.checked == false){
        for(let sub of chk.querySelectorAll('input[type=checkbox]')){
            if(sub.checked)
                toggleCollapse(sub);
        }
    }
}

function addCopyCodeButton(){
    var cb = document.querySelectorAll('.highlighter-rouge');
    for(let item of cb)
    {
        let ele = document.createElement('div');
        ele.className = 'copy-code';
        item.appendChild(ele);
        ele.addEventListener('click', function() {

            let pre = item.querySelector('pre').innerText;
            navigator.clipboard.writeText(pre);
            ele.className = 'copy-code copied';
            setTimeout(() => {
                ele.className = 'copy-code';
            }, 2000);
        });
    }
}