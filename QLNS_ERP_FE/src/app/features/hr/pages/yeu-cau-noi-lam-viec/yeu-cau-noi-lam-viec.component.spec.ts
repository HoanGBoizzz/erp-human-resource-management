import { ComponentFixture, TestBed } from '@angular/core/testing';
import { YeuCauNoiLamViecComponent } from './yeu-cau-noi-lam-viec.component';

describe('YeuCauNoiLamViecComponent', () => {
  let component: YeuCauNoiLamViecComponent;
  let fixture: ComponentFixture<YeuCauNoiLamViecComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [YeuCauNoiLamViecComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(YeuCauNoiLamViecComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
