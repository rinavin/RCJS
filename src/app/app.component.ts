import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';



declare var myExtObject: any;
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],

})


export class AppComponent implements OnInit {

  id: string;
  name: string;
  user: FormGroup;
   Myuser : User;

  constructor(private ref: ChangeDetectorRef) { }

  ngOnInit() {
    
    this.user = new FormGroup({
      name: new FormControl(''),    
      id: new FormControl(''),
       
     
    });
    this.initializeMagic();
  }

  initializeMagic() {

    var self = this;
    myExtObject.startMagic(data => {
      var obj = JSON.parse(data);
      //alert(data);
     // self.id = obj[1].Value;
     // self.name = obj[3].Value;
       (<FormControl>this.user.controls['id'])
    .setValue(obj[1].Value, { onlySelf: true });
     (<FormControl>this.user.controls['name'])
    .setValue(obj[3].Value, { onlySelf: true });
      self.ref.detectChanges();
    }
    );
  }

  buttonClick(index: number) {
   (<FormControl>this.user.controls['name'])
    .setValue('John', { onlySelf: true });
    myExtObject.buttonClick(index);
  }


}
export interface User {
  id : string;
  name: string;
  
}
