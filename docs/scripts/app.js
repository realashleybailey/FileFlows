document.addEventListener("DOMContentLoaded", function()
{
    var checkboxes = document.querySelectorAll('.side-bar input[type=checkbox]');
    for(let chk of checkboxes){
        let id = chk.id;
        if(localStorage.getItem('collapse_' + id) == 1){
            chk.checked = true;
        }
    }
});

function toggleCollapse(event){
    let id = event.target.id;
    console.log('event.target.id: ', event.target.id);
    console.log('event.target.checked: ', event.target.checked);
    localStorage.setItem('collapse_' + id, event.target.checked ? 1 : 0);

}